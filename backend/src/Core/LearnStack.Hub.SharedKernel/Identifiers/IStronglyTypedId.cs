namespace LearnStack.Hub.SharedKernel.Identifiers;

public interface IStronglyTypedId<out TKey>
    where TKey : notnull
{
    TKey Value { get; }
}
