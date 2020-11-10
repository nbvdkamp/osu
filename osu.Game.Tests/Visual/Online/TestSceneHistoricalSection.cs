// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile.Sections;
using osu.Game.Users;

using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.Profile;

namespace osu.Game.Tests.Visual.Online
{
    [TestFixture]
    public class TestSceneHistoricalSection : OsuTestScene
    {
        protected override bool UseOnlineAPI => true;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Pink);

        [Resolved]
        private IAPIProvider api { get; set; }

        private readonly HistoricalSection section; 
        public TestSceneHistoricalSection()
        {
            Add(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = OsuColour.Gray(0.2f)
            });

            Add(new OsuScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = section = new HistoricalSection(),
            });

            AddStep("Show peppy", () => section.User.Value = new User { Id = 2 });
            AddStep("Show WubWoofWolf", () => section.User.Value = new User { Id = 39828 });

            addOnlineStep("Show ppy (with network)", new User
            {
                Username = @"peppy",
                Id = 2,
                IsSupporter = true,
                Country = new Country { FullName = @"Australia", FlagName = @"AU" },
                CoverUrl = @"https://osu.ppy.sh/images/headers/profile-covers/c3.jpg"
            });
        }

        private void addOnlineStep(string name, User fallback)
        {
            AddStep(name, () =>
            {
                if (api.IsLoggedIn)
                {
                    var request = new GetUserRequest(fallback.Id);
                    request.Success += user => {
                        section.User.Value = user;
                    };
                    api.Queue(request);
                }
                else
                    section.User.Value = fallback;
            });
        }
    }
}
