namespace AgropecuarIA.CapacityPlanningSpike;

public enum CapacityPlanningErrorCode
{
    InvalidInput,
    CalculationOverflow,
}

public sealed class CapacityPlanningException : ArgumentException
{
    public CapacityPlanningException(
        CapacityPlanningErrorCode code,
        string parameterName,
        string message,
        Exception? innerException = null)
        : base(message, parameterName, innerException)
    {
        Code = code;
    }

    public CapacityPlanningErrorCode Code { get; }
}
