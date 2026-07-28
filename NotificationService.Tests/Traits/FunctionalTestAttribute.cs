using Xunit.Sdk;

namespace NotificationService.Tests.Traits;

[TraitDiscoverer("NotificationService.Tests.Traits.FunctionalTestDiscoverer", "NotificationService.Tests")]
[AttributeUsage(AttributeTargets.Class)]
public sealed class FunctionalTestAttribute : Attribute, ITraitAttribute;
