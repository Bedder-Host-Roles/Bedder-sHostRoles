using System.IO;

namespace BHR;

public class DevUser(string code = "", string color = "null", string userType = "null", string tag = "null", bool isUp = false, bool isDev = false, bool deBug = false, bool colorCmd = false, bool nameCmd = false, string upName = "未认证用户")
{
    public string Code { get; set; } = code;
    public string Color { get; set; } = color;
    public string UserType { get; set; } = userType;
    public string Tag { get; set; } = tag;
    public bool IsUp { get; set; } = isUp;
    public bool IsDev { get; set; } = isDev;
    public bool DeBug { get; set; } = deBug;
    public bool ColorCmd { get; set; } = colorCmd;
    public bool NameCmd { get; set; } = nameCmd;
    public string UpName { get; set; } = upName;

    public bool HasTag() => Tag != "null";
    //public string GetTag() => Color == "null" ? $"<size=1.2>{Tag}</size>\r\n" : $"<color={Color}><size=1.2>{(Tag == "#Dev" ? Translator.GetString("Developer") : Tag)}</size></color>\r\n";
    public string GetTag()
    {
#if ANDROID
        string tagColorFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "BHR-DATA", "Tags", "SPONSOR_TAGS", $"{Code}.txt");
#else
        string tagColorFilePath = @$"./BHR-DATA/Tags/SPONSOR_TAGS/{Code}.txt";
#endif
        if (Color == "null" || Color == string.Empty) return $"<size=1.2>{Tag}</size>\r\n";
        var startColor = Color.TrimStart('#');

        if (File.Exists(tagColorFilePath))
        {
            var ColorCode = File.ReadAllText(tagColorFilePath);
            if (Utils.CheckColorHex(ColorCode)) startColor = ColorCode;
        }
        string t1;
        t1 = Tag == "#Dev" ? Translator.GetString("Developer") : Tag;
        return $"<size=1.2><color=#{startColor}>{t1}</color></size>\r\r\n";
    }
    //public string GetTag() 
    //{
    //    string tagColorFilePath = @$"./BHR-DATA/Tags/SPONSOR_TAGS/{Code}.txt";

    //    if (Color == "null" || Color == string.Empty) return $"<size=1.2>{Tag}</size>\r\n";
    //    var startColor = "FFFF00";
    //    var endColor = "FFFF00";
    //    var startColor1 = startColor;
    //    var endColor1 = endColor;
    //    if (Color.Split(",").Length == 1)
    //    {
    //        startColor1 = Color.Split(",")[0].TrimStart('#');
    //        endColor1 = startColor1;
    //    }
    //    else if (Color.Split(",").Length == 2)
    //    {
    //         startColor1 = Color.Split(",")[0].TrimStart('#');
    //         endColor1 = Color.Split(",")[1].TrimStart('#');
    //    }
    //    if (File.Exists(tagColorFilePath))
    //    {
    //        var ColorCode = File.ReadAllText(tagColorFilePath);
    //        if (ColorCode.Split(" ").Length == 2)
    //        {
    //            startColor = ColorCode.Split(" ")[0];
    //            endColor = ColorCode.Split(" ")[1];
    //        }
    //        else
    //        {
    //            startColor = startColor1;
    //            endColor = endColor1;
    //        }
    //    }
    //    else
    //    {
    //        startColor = startColor1;
    //        endColor = endColor1;
    //    }
    //    if (!Utils.CheckGradientCode($"{startColor} {endColor}"))
    //    {
    //        startColor = "FFFF00";
    //        endColor = "FFFF00";
    //    }
    //    var t1 = "";
    //    t1 = Tag == "#Dev" ? Translator.GetString("Developer") : Tag;
    //    return $"<size=1.2>{Utils.GradientColorText(startColor,endColor, t1)}</size>\r\n";
    //}
}

public static class DevManager
{
    private readonly static DevUser DefaultDevUser = new();
    public readonly static List<DevUser> DevUserList = [];
    public static void Init()
    {
        // Dev
        DevUserList.Add(new(code: "actorour#0029", color: "#ffc0cb", tag: "Original Developer", userType: "s_cr", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "KARPED1EM"));
        DevUserList.Add(new(code: "pinklaze#1776", color: "#30548e", tag: "#Dev", userType: "s_cr", isUp: true, isDev: true, deBug: true, colorCmd: false, upName: "NCSIMON"));
        DevUserList.Add(new(code: "icingposh#6469", color: "#9e2424", userType: "s_cr", tag: "discord.gg/BHR", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "ryuk2"));
        DevUserList.Add(new(code: "bestanswer#3360", color: "#00ff1d", tag: "绿色游戏", userType: "s_cr", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: null)); //NikoCat233's alt
        DevUserList.Add(new(code: "happypride#3747", color: "#00ff1d", tag: "绿色游戏", userType: "s_cr", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: null)); //NikoCat233's alt 2
        DevUserList.Add(new(code: "unseenray#6185", color: "#33ff00", tag: "#Dev", userType: "s_cr", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "BatmenzDW"));

        // Mods
        DevUserList.Add(new(code: "manenet#9304", color: "#0004ff", tag: "Kush Tush", userType: "s_md", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: "Kush"));
    }
    public static bool IsDevUser(this string code) => DevUserList.Any(x => x.Code == code);
    public static DevUser GetDevUser(this string code) => code.IsDevUser() ? DevUserList.Find(x => x.Code == code) : DefaultDevUser;
    public static string GetUserType(this DevUser user)
    {
        string rolename = "Crewmate";

        if (user.UserType != "null" && user.UserType != string.Empty)
        {
            switch (user.UserType)
            {
                case "s_cr":
                    rolename = "<color=#ff0000>Contributor</color>";
                    break;
                case "s_bo":
                    rolename = "<color=#7f00ff>Booster</color>";
                    break;
                case "s_tr":
                    rolename = "<color=#f46e6e>Tester</color>";
                    break;
                case "s_jc":
                    rolename = "<color=#f46e6e>Junior Contributor</color>";
                    break;
                case "s_md":
                    rolename = "<color=#f46e6e>Moderator</color>";
                    break;

                default:
                    if (user.UserType.StartsWith("s_"))
                    {
                        rolename = "<color=#ffff00>Sponsor</color>";
                    }
                    else if (user.UserType.StartsWith("t_"))
                    {
                        rolename = "<color=#00ffff>Translator</color>";
                    }
                    break;
            }
        }

        return rolename;
    }

}
