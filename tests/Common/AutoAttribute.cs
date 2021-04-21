using System;
using System.IO;
using System.Linq;
using System.Text.Json;

using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

using Microsoft.Extensions.Options;

using PuppeteerSharp;

internal class AutoAttribute : AutoDataAttribute
{
    public AutoAttribute() : base(Create) { }

    public static IFixture Create()
    {
        var fixture = new Fixture();

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
