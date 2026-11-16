
namespace MegaSena.Api.Services;

/// <summary>Limita quantos cálculos pesados (fechamentos) rodam ao mesmo tempo no servidor.</summary>
public interface ISolverGate
{
    /// <summary>Devolve uma vaga (descarte-a ao terminar) ou null se o servidor estiver ocupado.</summary>
    IDisposable? TryEnter();

    /// <summary>
    /// Igual a <see cref="TryEnter()"/>, mas também reserva <paramref name="bytes"/> do orçamento de memória
    /// (Solver:MemoriaMaximaMb). Implementações que não controlam memória caem no comportamento simples.
    /// </summary>
    IDisposable? TryEnter(long bytes) => TryEnter();

    /// <summary>Orçamento total de memória dos fechamentos em bytes; 0 = sem controle por memória.</summary>
    long OrcamentoBytes => 0;
}

/// <summary>
/// Limita quantas simulações físicas pesadas (CPU) rodam ao mesmo tempo, sem disputar vagas com os fechamentos.
/// Usado por /uniformidade?fisico=true.
/// </summary>
public interface ISimulacaoGate
{
    IDisposable? TryEnter();
}

public sealed class SolverGate : ISolverGate, IDisposable
{
    private readonly int _max;
    private readonly long _orcamentoBytes;
    private readonly object _trava = new();
    private int _ativos;
    private long _emUsoBytes;

    public SolverGate(IConfiguration configuration)
    {
        // Máximo de fechamentos simultâneos (padrão 2)
        _max = Math.Clamp(configuration.GetValue("Solver:MaxConcorrentes", 2), 1, 16);

        // Orçamento de memória dos fechamentos, em MB. 0 (padrão) desliga o controle por memória.
        // Um fechamento de 20 dezenas usa cerca de 300 MB de pico.
        int mb = Math.Max(0, configuration.GetValue("Solver:MemoriaMaximaMb", 0));
        _orcamentoBytes = mb * 1024L * 1024L;
    }

    public long OrcamentoBytes => _orcamentoBytes;

    public IDisposable? TryEnter() => TryEnter(0);

    public IDisposable? TryEnter(long bytes)
    {
        lock (_trava)
        {
            if (_ativos >= _max) return null;

            // Com outros fechamentos em andamento, só entra se couber no orçamento.
            // Sozinho, um fechamento sempre entra: o controller já recusou os grandes demais.
            if (_orcamentoBytes > 0 && _ativos > 0 && _emUsoBytes + bytes > _orcamentoBytes)
                return null;

            _ativos++;
            _emUsoBytes += bytes;
            return new Vaga(this, bytes);
        }
    }

    private void Liberar(long bytes)
    {
        lock (_trava)
        {
            _ativos--;
            _emUsoBytes -= bytes;
        }
    }

    public void Dispose() { }

    private sealed class Vaga : IDisposable
    {
        private readonly SolverGate _gate;
        private readonly long _bytes;
        private int _liberada;

        public Vaga(SolverGate gate, long bytes)
        {
            _gate = gate;
            _bytes = bytes;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _liberada, 1) == 0)
                _gate.Liberar(_bytes);
        }
    }
}

public sealed class SimulacaoGate : ISimulacaoGate, IDisposable
{
    private readonly SemaphoreSlim _semaforo;

    public SimulacaoGate(IConfiguration configuration)
    {
        int max = Math.Clamp(configuration.GetValue("Simulacao:MaxConcorrentes", 1), 1, 8);
        _semaforo = new SemaphoreSlim(max, max);
    }

    public IDisposable? TryEnter() => _semaforo.Wait(0) ? new Vaga(_semaforo) : null;

    public void Dispose() => _semaforo.Dispose();

    private sealed class Vaga : IDisposable
    {
        private readonly SemaphoreSlim _semaforo;
        private int _liberada;

        public Vaga(SemaphoreSlim semaforo) => _semaforo = semaforo;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _liberada, 1) == 0)
                _semaforo.Release();
        }
    }
}
