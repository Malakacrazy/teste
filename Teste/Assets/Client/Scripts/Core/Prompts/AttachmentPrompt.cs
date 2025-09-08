using UnityEngine;

namespace L5RGame
{
    public class AttachmentPrompt : UiPrompt
    {
        public Player player;
        public BaseCard attachmentCard;
        public string playingType;

        public AttachmentPrompt(Game game, Player player, BaseCard attachmentCard, string playingType) : base(game)
        {
            this.player = player;
            this.attachmentCard = attachmentCard;
            this.playingType = playingType;
        }

        public override bool Continue()
        {
            game.PromptForSelect(player, new PromptProperties
            {
                source = new EffectSource(game, "Play Attachment"),
                activePromptTitle = "Select target for attachment",
                controller = Players.Self,
                gameAction = GameActions.Attach(new AttachProperties { attachment = attachmentCard }),
                onSelect = (selectPlayer, card) =>
                {
                    var context = new AbilityContext(game, selectPlayer, card);
                    GameActions.Attach(new AttachProperties { attachment = attachmentCard })
                        .Resolve(card, context);
                    return true;
                }
            });
            return true;
        }
    }
}
