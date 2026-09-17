// Asset types used by ObjectDetailsTests. They deliberately live in specific namespaces
// (and one in the global namespace) so the assembly / namespace / location comparison
// logic inside ObjectDetails can be exercised.
// Global-namespace type: has no namespace, so ObjectDetails.Location is null.


#pragma warning disable IDE0130 // Namespace does not match folder structure
internal sealed class GlobalNamespaceAsset
{
}

namespace MediatR.Tests.ObjectDetailsTestAssets.NsCompare
{
    internal sealed class NsCompareRequest
    {
    }

    internal sealed class NsCompareHandler
    {
    }
}

namespace MediatR.Tests.ObjectDetailsTestAssets.Other
{
    internal sealed class OtherNamespaceHandler
    {
    }
}

namespace MediatR.Tests.ObjectDetailsTestAssets.YetAnother
{
    internal sealed class YetAnotherNamespaceHandler
    {
    }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure