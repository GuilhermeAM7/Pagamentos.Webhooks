namespace Application.Domain.Enums
{
    public static class ConversorPagamentoStatus
    {
        public static PagamentoStatus Parse(string? valor) =>
            TryParse(valor, out var status) ? status : PagamentoStatus.Unknown;

        public static bool TryParse(string? valor, out PagamentoStatus status)
        {
            status = PagamentoStatus.Unknown;

            if (string.IsNullOrWhiteSpace(valor))
                return false;

            if (Enum.TryParse<PagamentoStatus>(valor, ignoreCase: true, out var parsed)
                && Enum.IsDefined(parsed))
            {
                status = parsed;
                return true;
            }

            return false;
        }
    }
}
