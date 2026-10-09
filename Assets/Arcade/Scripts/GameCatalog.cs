namespace Arcade
{
    /// <summary>One ordered source of truth for menus, scene loading and tests.</summary>
    public static class GameCatalog
    {
        public const string MenuScene = "Assets/Arcade/Scenes/MainMenu.unity";
        public const string Title = "ARCADE TRIO";
        public const string Author = "Zwe Khant Lin";
        public const string StudentId = "6632710";
        public static readonly string[] Scenes = {
            "Assets/Games/Dogs/Challenge 2/Challenge 2.unity",
            "Assets/Games/Balloon/Challenge 3/Challenge 3.unity",
            "Assets/Games/Arena/Challenge 4/Challenge 4.unity"
        };
        public static readonly string[] Titles = { "Dog Patrol", "Fly Like a Balloon", "Sumo Ball Arena" };
        public static readonly string[] Descriptions = {
            "CHALLENGE 2  /  Catch the falling balls",
            "CHALLENGE 3  /  Collect money. Dodge bombs.",
            "CHALLENGE 4  /  Push back the next wave"
        };
        public static readonly string[] Controls = {
            "W / S  or  ↑ / ↓  move     ·     SPACE  send a dog",
            "HOLD SPACE  rise     ·     RELEASE  descend     ·     Avoid bombs",
            "W / S  move     ·     A / D  steer     ·     SPACE  turbo"
        };
    }
}
