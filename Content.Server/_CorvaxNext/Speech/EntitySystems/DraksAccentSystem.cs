using System.Text.RegularExpressions;
using Content.Server._CorvaxNext.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class DraksAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DraksAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, DraksAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // о => оо
        message = Regex.Replace(
            message,
            "о+",
            "оо"
        );
        // О => ОО
        message = Regex.Replace(
            message,
            "О+",
            "ОО"
        );
        // у => уу
        message = Regex.Replace(
            message,
            "у+",
            "уу"
        );
        // У => УУ
        message = Regex.Replace(
            message,
            "У+",
            "УУ"
        );
        // м => мм
        message = Regex.Replace(
            message,
            "м+",
            "мм"
        );
        // М => ММ
        message = Regex.Replace(
            message,
            "М+",
            "ММ"
        );
        // и => ии
        message = Regex.Replace(
            message,
            "и+",
            "ии"
        );
        // И => ИИ
        message = Regex.Replace(
            message,
            "И+",
            "ИИ"
        );
        // с => сс
        message = Regex.Replace(
            message,
            "с+",
            "сс"
        );
        // С => СС
        message = Regex.Replace(
            message,
            "С+",
            "СС"
        );
        args.Message = message;
    }
}
