namespace JL.Core.Frontend;

public static class FrontendManager
{
    public static IFrontend Frontend { get; set; } = new DummyFrontend();
}
