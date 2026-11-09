import pandas as pd
import uuid
import re

ARQUIVO_EXCEL = 'Mega-Sena.xlsx'
NOME_ABA = 'MEGA SENA'
ARQUIVO_SAIDA = 'inserts_mega_sena.sql'

def escapar_texto(texto):
    """Escapa apóstrofos para o SQL Server (ex: D'OESTE -> D''OESTE)"""
    if pd.isna(texto) or str(texto).strip() == '':
        return 'NULL'
    texto_limpo = str(texto).strip().replace("'", "''")
    return f"'{texto_limpo}'"

def limpar_moeda(valor):
    if pd.isna(valor) or str(valor).strip() == '':
        return 'NULL'
    try:
        s = str(valor).replace('R$', '').replace('.', '').replace(',', '.').strip()
        return f"{float(s):.2f}"
    except ValueError:
        return 'NULL'

def formatar_data(data_excel):
    try:
        dt = pd.to_datetime(data_excel, dayfirst=True)
        return dt.strftime('%Y-%m-%d 20:00:00')
    except:
        return 'NULL'

def separar_cidade_uf(texto_cidade_uf):
    if pd.isna(texto_cidade_uf) or str(texto_cidade_uf).strip() == '':
        return 'NULL', 'NULL'
    
    texto = str(texto_cidade_uf).strip().upper()
    
    # Se tem múltiplas UFs (ex: RJ;MG), pega apenas a primeira
    if ';' in texto:
        primeira_uf = texto.split(';')[0].strip()
        # Se for apenas a UF (2 letras), retorna ela
        if len(primeira_uf) == 2:
            return 'NULL', escapar_texto(primeira_uf)
        # Se tiver cidade junto com a UF (ex: SAO PAULO/RJ; MG), tenta separar
        if '/' in primeira_uf:
            partes = primeira_uf.split('/')
            return escapar_texto(partes[0].strip()), escapar_texto(partes[-1].strip())

    # Se tiver apenas uma UF (ex: SP)
    if len(texto) == 2:
        return 'NULL', escapar_texto(texto)
    
    # Se tiver cidade/UF (ex: SAO PAULO/SP)
    if '/' in texto:
        partes = texto.split('/')
        uf = partes[-1].strip()
        cidade = '/'.join(partes[:-1]).strip()
        return escapar_texto(cidade), escapar_texto(uf)
        
    # Se tiver cidade - UF
    if ' - ' in texto:
        partes = texto.split(' - ')
        uf = partes[-1].strip()
        cidade = ' - '.join(partes[:-1]).strip()
        return escapar_texto(cidade), escapar_texto(uf)

    # Fallback: Se a string for maior que 2 e não cair nos casos acima, joga os 2 primeiros como UF
    return 'NULL', escapar_texto(texto[:2])

def gerar_sql():
    print(f"Lendo arquivo {ARQUIVO_EXCEL}...")
    df = pd.read_excel(ARQUIVO_EXCEL, sheet_name=NOME_ABA, header=1)
    
    df.columns = [
        "Concurso", "Data", "B1", "B2", "B3", "B4", "B5", "B6", 
        "G6", "CidadeUF", "R6", "G5", "R5", "G4", "R4", 
        "Acumulado6", "Arrecadacao", "Estimativa", "AcumuladoVirada", "Observacao"
    ]

    inserts = []
    
    print("Processando dados...")
    for index, row in df.iterrows():
        try:
            if pd.isna(row['Concurso']) or str(row['Concurso']).strip() == '':
                continue

            id_uuid = str(uuid.uuid4())
            num_concurso = int(row['Concurso'])
            dt_sorteio = formatar_data(row['Data'])
            
            cidade, uf = separar_cidade_uf(row['CidadeUF'])
            
            bolas = sorted([int(row['B1']), int(row['B2']), int(row['B3']), int(row['B4']), int(row['B5']), int(row['B6'])])
            dezenas = ",".join([f"{b:02d}" for b in bolas])
            
            g6 = int(row['G6']) if not pd.isna(row['G6']) else 0
            g5 = int(row['G5']) if not pd.isna(row['G5']) else 0
            g4 = int(row['G4']) if not pd.isna(row['G4']) else 0
            
            premio6 = limpar_moeda(row['R6'])
            premio5 = limpar_moeda(row['R5'])
            premio4 = limpar_moeda(row['R4'])
            arrecadacao = limpar_moeda(row['Arrecadacao'])
            acumulado6_valor = limpar_moeda(row['Acumulado6'])
            
            acumulado_bit = 1 if g6 == 0 else 0
            json_rateio = 'NULL'
            
            num_anterior = num_concurso - 1 if num_concurso > 1 else 'NULL'
            prox_concurso = num_concurso + 1
            
            dt_importacao = "SYSDATETIME()"

            sql = (f"INSERT INTO dbo.Concurso (Id, NumeroConcurso, DataHoraSorteio, CidadeSorteio, UfSorteio, "
                   f"DezenasSorteadas, QtdGanhadoresSena, QtdGanhadoresQuina, QtdGanhadoresQuadra, "
                   f"ValorPremioSena, ValorPremioQuina, ValorPremioQuadra, ValorArrecadado, Acumulado, "
                   f"ValorAcumulado, ListaRateioPremioJson, NumeroConcursoAnterior, ProximoConcurso, DataImportacao) "
                   f"VALUES ('{id_uuid}', {num_concurso}, '{dt_sorteio}', {cidade}, {uf}, "
                   f"'{dezenas}', {g6}, {g5}, {g4}, "
                   f"{premio6}, {premio5}, {premio4}, {arrecadacao}, {acumulado_bit}, "
                   f"{acumulado6_valor}, {json_rateio}, {num_anterior}, {prox_concurso}, {dt_importacao});")
            
            inserts.append(sql)
            
        except Exception as e:
            print(f"Erro no concurso {row.get('Concurso', 'Desconhecido')}: {e}")

    print(f"Salvando {len(inserts)} registros no arquivo {ARQUIVO_SAIDA}...")
    with open(ARQUIVO_SAIDA, 'w', encoding='utf-8') as f:
        f.write("SET NOCOUNT ON;\n\n")
        for i in range(0, len(inserts), 100):
            f.write("\n".join(inserts[i:i+100]) + "\n")
            f.write("GO\n")
            
    print("Concluído com sucesso!")

if __name__ == "__main__":
    gerar_sql()