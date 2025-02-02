﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.Chat;
using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using System.Collections.Generic;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Graphics.Sprites;
using osu.Game.Users;

namespace osu.Game.Graphics.Containers
{
    public class LinkFlowContainer : OsuTextFlowContainer
    {
        public LinkFlowContainer(Action<SpriteText> defaultCreationParameters = null)
            : base(defaultCreationParameters)
        {
        }

        [Resolved(CanBeNull = true)]
        private OsuGame game { get; set; }

        [Resolved]
        private GameHost host { get; set; }

        public void AddLinks(string text, List<Link> links)
        {
            if (string.IsNullOrEmpty(text) || links == null)
                return;

            if (links.Count == 0)
            {
                AddText(text);
                return;
            }

            int previousLinkEnd = 0;

            foreach (var link in links)
            {
                AddText(text[previousLinkEnd..link.Index]);

                string displayText = text.Substring(link.Index, link.Length);
                string linkArgument = link.Argument;
                string tooltip = displayText == link.Url ? null : link.Url;

                AddLink(displayText, link.Action, linkArgument, tooltip);
                previousLinkEnd = link.Index + link.Length;
            }

            AddText(text.Substring(previousLinkEnd));
        }

        public void AddLink(string text, string url, Action<SpriteText> creationParameters = null) =>
            createLink(AddText(text, creationParameters), new LinkDetails(LinkAction.External, url), url);

        public void AddLink(string text, Action action, string tooltipText = null, Action<SpriteText> creationParameters = null)
            => createLink(AddText(text, creationParameters), new LinkDetails(LinkAction.Custom, string.Empty), tooltipText, action);

        public void AddLink(string text, LinkAction action, string argument, string tooltipText = null, Action<SpriteText> creationParameters = null)
            => createLink(AddText(text, creationParameters), new LinkDetails(action, argument), tooltipText);

        public void AddLink(LocalisableString text, LinkAction action, string argument, string tooltipText = null, Action<SpriteText> creationParameters = null)
        {
            var spriteText = new OsuSpriteText { Text = text };

            AddText(spriteText, creationParameters);
            createLink(spriteText.Yield(), new LinkDetails(action, argument), tooltipText);
        }

        public void AddLink(IEnumerable<SpriteText> text, LinkAction action, string linkArgument, string tooltipText = null)
        {
            foreach (var t in text)
                AddArbitraryDrawable(t);

            createLink(text, new LinkDetails(action, linkArgument), tooltipText);
        }

        public void AddUserLink(User user, Action<SpriteText> creationParameters = null)
            => createLink(AddText(user.Username, creationParameters), new LinkDetails(LinkAction.OpenUserProfile, user.Id.ToString()), "view profile");

        private void createLink(IEnumerable<Drawable> drawables, LinkDetails link, string tooltipText, Action action = null)
        {
            var linkCompiler = CreateLinkCompiler(drawables.OfType<SpriteText>());
            linkCompiler.RelativeSizeAxes = Axes.Both;
            linkCompiler.TooltipText = tooltipText;
            linkCompiler.Action = () =>
            {
                if (action != null)
                    action();
                else if (game != null)
                    game.HandleLink(link);
                // fallback to handle cases where OsuGame is not available, ie. tournament client.
                else if (link.Action == LinkAction.External)
                    host.OpenUrlExternally(link.Argument);
            };

            AddInternal(linkCompiler);
        }

        protected virtual DrawableLinkCompiler CreateLinkCompiler(IEnumerable<SpriteText> parts) => new DrawableLinkCompiler(parts);

        // We want the compilers to always be visible no matter where they are, so RelativeSizeAxes is used.
        // However due to https://github.com/ppy/osu-framework/issues/2073, it's possible for the compilers to be relative size in the flow's auto-size axes - an unsupported operation.
        // Since the compilers don't display any content and don't affect the layout, it's simplest to exclude them from the flow.
        public override IEnumerable<Drawable> FlowingChildren => base.FlowingChildren.Where(c => !(c is DrawableLinkCompiler));
    }
}
