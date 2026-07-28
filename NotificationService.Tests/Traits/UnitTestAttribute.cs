using Xunit.Sdk;

namespace NotificationService.Tests.Traits;

[TraitDiscoverer("NotificationService.Tests.Traits.UnitTestDiscoverer", "NotificationService.Tests")]
[AttributeUsage(AttributeTargets.Class)]
public sealed class UnitTestAttribute : Attribute, ITraitAttribute;
