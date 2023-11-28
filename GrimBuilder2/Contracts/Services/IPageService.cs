namespace GrimBuilder2.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);
    IEnumerable<Type> EnumeratePageTypes();
}
