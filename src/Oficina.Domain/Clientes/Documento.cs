using Oficina.Domain.Shared;

namespace Oficina.Domain.Clientes;

public enum TipoDocumento { CPF, CNPJ }

/// <summary>
/// Value Object de documento do cliente. Aceita CPF (11) ou CNPJ (14),
/// validando os digitos verificadores. Atende ao requisito de validacao
/// de CPF/CNPJ do PDF.
/// </summary>
public sealed class Documento
{
    public string Numero { get; }   // somente digitos, normalizado
    public TipoDocumento Tipo { get; }

    private Documento(string numero, TipoDocumento tipo)
    {
        Numero = numero;
        Tipo = tipo;
    }

    public static Documento Criar(string entrada)
    {
        var digitos = new string((entrada ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digitos.Length == 11)
        {
            if (!CpfValido(digitos))
                throw new DomainException("CPF invalido.");
            return new Documento(digitos, TipoDocumento.CPF);
        }

        if (digitos.Length == 14)
        {
            if (!CnpjValido(digitos))
                throw new DomainException("CNPJ invalido.");
            return new Documento(digitos, TipoDocumento.CNPJ);
        }

        throw new DomainException("Documento deve ter 11 digitos (CPF) ou 14 digitos (CNPJ).");
    }

    private static bool CpfValido(string cpf)
    {
        if (cpf.Distinct().Count() == 1) return false;

        int Digito(string parcial)
        {
            int soma = 0, peso = parcial.Length + 1;
            foreach (var c in parcial) soma += (c - '0') * peso--;
            int resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        int d1 = Digito(cpf[..9]);
        int d2 = Digito(cpf[..9] + d1);
        return cpf.EndsWith($"{d1}{d2}");
    }

    private static bool CnpjValido(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1) return false;

        int[] p1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] p2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        int Digito(string parcial, int[] pesos)
        {
            int soma = 0;
            for (int i = 0; i < parcial.Length; i++) soma += (parcial[i] - '0') * pesos[i];
            int resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        int d1 = Digito(cnpj[..12], p1);
        int d2 = Digito(cnpj[..12] + d1, p2);
        return cnpj.EndsWith($"{d1}{d2}");
    }

    public override bool Equals(object? obj) => obj is Documento d && d.Numero == Numero;
    public override int GetHashCode() => Numero.GetHashCode();
    public override string ToString() => Numero;
}
