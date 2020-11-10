// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.Profile.Sections.Historical;
using osu.Game.Overlays.Profile.Sections.Ranks;


using osu.Game.Users;
using osu.Framework.Allocation;

namespace osu.Game.Overlays.Profile.Sections
{
    public class HistoricalSection : ProfileSection
    {
        public override string Title => "Historical";

        public override string Identifier => "historical";

        private HistoryCountGraph playCountGraph;
        private HistoryCountGraph replaysWatchedCountGraph;

        public HistoricalSection()
        {
            Children = new Drawable[]
            {
                playCountGraph = new HistoryCountGraph
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 130,
                    TooltipText = "Plays",
                },
                new PaginatedMostPlayedBeatmapContainer(User),
                new PaginatedScoreContainer(ScoreType.Recent, User, "Recent Plays (24h)", CounterVisibilityState.VisibleWhenZero),
                replaysWatchedCountGraph = new HistoryCountGraph
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 130,
                    TooltipText = "Replays Watched",
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            User.ValueChanged += e => updateDisplay(e.NewValue);
        }

        private void updateDisplay(User user)
        {
            playCountGraph.HistoryCounts.Value = user.MonthlyPlaycounts;
            replaysWatchedCountGraph.HistoryCounts.Value = user.ReplaysWatchedCounts;
        }
    }
}
