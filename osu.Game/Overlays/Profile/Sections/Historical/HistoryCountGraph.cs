// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Overlays.Profile.Sections.Historical
{
    public class HistoryCountGraph : Container, IHasCustomTooltip
    {
        private const float secondary_textsize = 13;
        private const float padding = 10;
        private const float fade_duration = 150;

        private readonly HistoryCountLineGraph graph;
        private readonly OsuSpriteText placeholder;

        private List<User.UserHistoryCount> values;
        private int hoveredIndex;
        public readonly Bindable<User.UserHistoryCount[]> HistoryCounts = new Bindable<User.UserHistoryCount[]>();

        public string TooltipText { get; set; }

        public HistoryCountGraph()
        {
            Padding = new MarginPadding { Vertical = padding };
            Children = new Drawable[]
            {
                placeholder = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = "placeholder",
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Regular)
                },
                graph = new HistoryCountLineGraph
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.Both,
                    Y = -secondary_textsize,
                    Alpha = 0,
                }
            };

            graph.OnBallMove += i => hoveredIndex = i;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            graph.LineColour = colours.Yellow;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            HistoryCounts.BindValueChanged(e => updateHistoryCounts(e.NewValue), true);
        }

        private void updateHistoryCounts(User.UserHistoryCount[] counts)
        {
            placeholder.FadeIn(fade_duration, Easing.Out);

            if (counts == null)
            {
                graph.FadeOut(fade_duration, Easing.Out);
                values = null;
                return;
            }

            values = counts.ToList();
            int toSkip = 0;
            
            // Add months with no values for display
            for (int i = 0; i < values.Count; i++)
            {
                i += toSkip;
                if (i == values.Count - 1)
                    break;

                DateTime first = values[i].Date;
                DateTime second = values[i + 1].Date;

                List<User.UserHistoryCount> toInsert = new List<User.UserHistoryCount>();

                for (int j = 1; DateTime.Compare(first.AddMonths(j), second) < 0; j++)
                {
                   toInsert.Add(new User.UserHistoryCount 
                   {
                       Date = first.AddMonths(j),
                       Count = 0,
                   });
                }

                values.InsertRange(i + 1, toInsert);
                toSkip = toInsert.Count;
            }

            if (values.Count > 1)
            {
                placeholder.FadeOut(fade_duration, Easing.Out);

                graph.DefaultValueCount = values.Count;
                graph.Values = values.Select(x => (float) x.Count);
            }

            graph.FadeTo(values.Count > 1 ? 1 : 0, fade_duration, Easing.Out);
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (values?.Count > 1)
            {
                graph.UpdateBallPosition(e.MousePosition.X);
                graph.ShowBar();
            }

            return base.OnHover(e);
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            if (values?.Count > 1)
                graph.UpdateBallPosition(e.MousePosition.X);

            return base.OnMouseMove(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (values?.Count > 1)
            {
                graph.HideBar();
            }

            base.OnHoverLost(e);
        }

        private class HistoryCountLineGraph : LineGraph
        {
            private readonly CircularContainer movingBall;
            private readonly Container bar;
            private readonly Box ballBg;
            private readonly Box line;

            public Action<int> OnBallMove;

            public HistoryCountLineGraph()
            {
                Add(bar = new Container
                {
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Alpha = 0,
                    RelativePositionAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        line = new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Y,
                            Width = 1.5f,
                        },
                        movingBall = new CircularContainer
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(18),
                            Masking = true,
                            BorderThickness = 4,
                            RelativePositionAxes = Axes.Y,
                            Child = ballBg = new Box { RelativeSizeAxes = Axes.Both }
                        }
                    }
                });
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider, OsuColour colours)
            {
                ballBg.Colour = colourProvider.Background5;
                movingBall.BorderColour = line.Colour = colours.Yellow;
            }

            public void UpdateBallPosition(float mouseXPosition)
            {
                const int duration = 200;
                int index = calculateIndex(mouseXPosition);

                Vector2 position = calculateBallPosition(index);
                movingBall.MoveToY(position.Y, duration, Easing.OutQuint);
                bar.MoveToX(position.X, duration, Easing.OutQuint);
                OnBallMove.Invoke(index);
            }

            public void ShowBar() => bar.FadeIn(fade_duration);

            public void HideBar() => bar.FadeOut(fade_duration);

            private int calculateIndex(float mouseXPosition) => (int)MathF.Round(mouseXPosition / DrawWidth * (DefaultValueCount - 1));

            private Vector2 calculateBallPosition(int index)
            {
                float y = GetYPosition(Values.ElementAt(index));
                return new Vector2(index / (float)(DefaultValueCount - 1), y);
            }
        }

        public object TooltipContent
        {
            get
            {
                if (HistoryCounts.Value == null)
                    return null;

                User.UserHistoryCount value = values[hoveredIndex];

                return new TooltipDisplayContent
                {
                    Count = $"{value.Count:#,##0}",
                    Month = $"{value.Date.ToString("MMMM yyyy")}"
                };
            }
        }

        //TODO: this is only called once, not per object, resulting in all graphs having the same tooltip (see https://github.com/ppy/osu-framework/issues/3231)
        public ITooltip GetCustomTooltip() => new HistoryCountGraphTooltip(TooltipText);

        private class HistoryCountGraphTooltip : VisibilityContainer, ITooltip
        {
            private readonly OsuSpriteText globalRankingText, timeText;
            private readonly Box background;

            public HistoryCountGraphTooltip(string tooltipText)
            {
                AutoSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 10;

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(10),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                                        Text = $"{tooltipText} ",
                                    },
                                    globalRankingText = new OsuSpriteText
                                    {
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Regular),
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                    }
                                }
                            },
                            timeText = new OsuSpriteText
                            {
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.Regular),
                            }
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                // Temporary colour since it's currently impossible to change it without bugs (see https://github.com/ppy/osu-framework/issues/3231)
                // If above is fixed, this should use OverlayColourProvider
                background.Colour = colours.Gray1;
            }

            public bool SetContent(object content)
            {
                if (!(content is TooltipDisplayContent info))
                    return false;

                globalRankingText.Text = info.Count;
                timeText.Text = info.Month;
                return true;
            }

            private bool instantMove = true;

            public void Move(Vector2 pos)
            {
                if (instantMove)
                {
                    Position = pos;
                    instantMove = false;
                }
                else
                    this.MoveTo(pos, 200, Easing.OutQuint);
            }

            protected override void PopIn()
            {
                instantMove |= !IsPresent;
                this.FadeIn(200, Easing.OutQuint);
            }

            protected override void PopOut() => this.FadeOut(200, Easing.OutQuint);
        }

        private class TooltipDisplayContent
        {
            public string Count;
            public string Month;
        }
    }
}
