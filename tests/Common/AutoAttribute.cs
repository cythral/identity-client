using System.Linq;
using System.Text.Json;
using System.Threading;

using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

internal class AutoAttribute : AutoDataAttribute
{
    public AutoAttribute() : base(Create) { }

    public static IFixture Create()
    {
        var fixture = new Fixture();

        fixture.Inject(new CancellationToken(false));
        fixture.Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
        fixture.Customizations.Add(new TypeOmitter<JsonElement>());
        fixture.Customizations.Insert(-1, new TargetRelay());
        fixture.Behaviors
        .OfType<ThrowingRecursionBehavior>()
        .ToList()
        .ForEach(b => fixture.Behaviors.Remove(b));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

}
