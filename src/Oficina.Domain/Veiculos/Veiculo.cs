using Oficina.Domain.Shared;

namespace Oficina.Domain.Veiculos;

/// <summary>Raiz de agregado Veiculo (placa, marca, modelo, ano). Atende ao cadastro de veiculo do PDF.</summary>
public sealed class Veiculo : Entity
{
    public Guid ClienteId { get; private set; }
    public Placa Placa { get; private set; } = null!;
    public string Marca { get; private set; } = null!;
    public string Modelo { get; private set; } = null!;
    public int Ano { get; private set; }

    private Veiculo() { } // EF

    public Veiculo(Guid clienteId, Placa placa, string marca, string modelo, int ano)
    {
        if (clienteId == Guid.Empty)
            throw new DomainException("Veiculo deve pertencer a um cliente.");
        if (string.IsNullOrWhiteSpace(marca)) throw new DomainException("Marca e obrigatoria.");
        if (string.IsNullOrWhiteSpace(modelo)) throw new DomainException("Modelo e obrigatorio.");
        if (ano < 1900 || ano > DateTime.UtcNow.Year + 1)
            throw new DomainException("Ano do veiculo invalido.");

        ClienteId = clienteId;
        Placa = placa ?? throw new DomainException("Placa e obrigatoria.");
        Marca = marca.Trim();
        Modelo = modelo.Trim();
        Ano = ano;
    }
    public void Atualizar(string marca, string modelo, int ano)
    {
        if (string.IsNullOrWhiteSpace(marca)) throw new DomainException("Marca e obrigatoria.");
        if (string.IsNullOrWhiteSpace(modelo)) throw new DomainException("Modelo e obrigatorio.");
        if (ano < 1900 || ano > DateTime.UtcNow.Year + 1)
            throw new DomainException("Ano do veiculo invalido.");
        Marca = marca.Trim();
        Modelo = modelo.Trim();
        Ano = ano;
    }
}
