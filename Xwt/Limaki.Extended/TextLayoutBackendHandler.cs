namespace Xwt.Backends {
    public abstract partial class TextLayoutBackendHandler : DisposableResourceBackendHandler {
        public abstract void SetWrapMode(object backend, WrapMode value);
    }
}