namespace EvoltisTL.AuditDomain.Exceptions
{
    public class AuditableEntitySavingException : Exception
    {
        public AuditableEntitySavingException(IEnumerable<string> entityTypeNames) : base($"Entit{(entityTypeNames.Count() == 1 ? "y" : "ies")} of the type: {entityTypeNames.Aggregate(string.Empty, (actual, next) => actual + ", " + next)} {(entityTypeNames.Count() == 1 ? "is" : "are")} Auditable entities need the user id to be saved, add UserId to call the method")
        { }
    }
}