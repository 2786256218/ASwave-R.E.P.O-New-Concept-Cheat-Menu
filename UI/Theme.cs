using System;
using UnityEngine;

namespace Cheat.UI;

public static class Theme
{
	public static Color Background = GetColor(1184274);

	public static Color SidebarBackground = GetColor(1184274);

	public static Color ContentPanelBackground = GetColor(1973790);

	public static Color Accent = GetColor(65437);

	public static Color AccentSecondary = GetColor(47217);

	public static Color Text = GetColor(16777215);

	public static Color TextDim = GetColor(10526880);

	public static Color ItemBackground = GetColor(1973790);

	public static Color ItemHover = GetColor(2434341);

	public static Color Separator = GetColor(2960685);

	public static Color StatusError = GetColor(16731469);

	public static Color StatusWarning = GetColor(16761095);

	public static Color Shadow = new Color(0f, 0f, 0f, 0.8f);

	public static Color Glow = new Color(0f, 1f, 0.615f, 0.3f);

	public static Color IconActive = Accent;

	public static Color IconInactive = TextDim;

	public static Color IconHover = Color.white;

	public static float WindowWidth = 1100f;

	public static float WindowHeight = 650f;

	public static float SidebarWidth = 220f;

	public static float ContentMargin = 30f;

	public static float PanelWidth = 600f;

	public static float ItemHeight = 36f;

	public static float ItemSpacing = 10f;

	public static float CornerRadius = 12f;

	public static Texture2D WhiteTexture;

	public static Texture2D RoundedTexture;

	public static Texture2D CircleTexture;

	public static Texture2D ShadowTexture;

	public static Texture2D HueTexture;

	public static Texture2D SatValTexture;

	public static Texture2D IconLoot;

	public static Texture2D IconEnemies;

	public static Texture2D IconLocal;

	public static Texture2D IconMisc;

	public static Texture2D IconSettings;

	public static Texture2D CursorTexture;

	public static GUIStyle RoundedBoxStyle;

	private const string B64_Loot = "iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAAsTAAALEwEAmpwYAAADK0lEQVR4nO2YS0hWQRTHf1b2JLKU3m+L2peRJURRi4Je1KZFqwxXvRZlBBViEESLwKAWBi2iF7mLjN5IRFAkvZUsKsmKCssSSyRjYILbYb7vzh3nm0/CPwzcxfn/z//cO/fcMxf68P+hBDgK1AMtej0AqoCF9GIUAreA7ph1U8dmDU0WJl1XU4gCajJYQE2IAvZZGKkFlgNj9VoBXLbg7Q9RwOo0Bn4D5Wm45TomFX9NiAKmiqStQI6DjuK0Ci2lnXGYEk/xcCO+Ot4IJ9SJ5Ks8bMU6AqJKJN/roRlUERClIvkFD+24lIAoEslfePggFhEQQ4Aujx+wLmAogfHMYwHPQ5vP1ROmrwLqtWYQ9AfOeDTfrVdNiCJSmf8MLEigMxf4ELqIfsB5Q9JGYLqDnuI0GvTO6lzecciQ7CFQ0APNAq0hdQ/iGRsNSVQXGu1Be4zuQlFtNbFuwBNmAN9FgvfApBjeMqBam/sG/ASa9bFzsYidrDWjOdoct+Y/GADcEcLKSHEajnoqV2M6zkoDr1hrR+Nu68bhjO2G5GVp4vNTvJhypTrUlxlit7qaHwV8EWKXYmb2cwYDHfqJnNTHyndp7mqOPo7KFj3SpYAjQkgdYiakiZ9lMH8FGC/i4lrkRMOB6XBS83lAuxDZHcM5IOKfAoNwwx6hpZrIiCQCO4TAJ2B4iljbUSEJhgEfBX9LEoEGQd6ZJjYTBSjsMjxRKxQK4i/Lr230fKCue4p8nTvqZZoNcbMgXbRMGP3foxL7QK3wssmGdFqQtsXEZ2oLmd7FU1jgriCp3+XZKmCR0FBTQSxeCZL6t2mDasFr9HDOHSc0X9qQfgiSbS9fYrjr6mfVTBGXpKjBQk95i4X8gCkRW1w3FKE60mM9TjzSJnIdC2i3Ib1x3EJ/x4C3Fu/DbMct9NqGdF+Q1NYgYRHXYgpYa6m1VPDu2ZCOC1IFbijRQ9gN4Ik+OqrrE8A8S41K4eWYDWm9IDWH/GcTwUA9eke9rMNyEm3z9BR6gkrhoS3JRCpH496wKpJUn2c4ZGdztWhPiTDH8E3IxuoA5uMI9dJ0ZtF8p+2L2weyhD/xtMbE/Fm2bAAAAABJRU5ErkJggg==";

	private const string B64_Enemies = "iVBORw0KGgoAAAANSUhEUgAAADIAAAAyCAYAAAAeP4ixAAAACXBIWXMAAAsTAAALEwEAmpwYAAAA00lEQVR4nO2WQQ7DMAgE+f+zGuU7/cL24lwsuU3tBeN4R+IQWQZNAhFmYj1OM8PkOBgiSBLD0BLNro+dRNBZzDN3V6J3iSVFQCzyLRcksvOwewKJ7CaCjrVi5M4wjxe56F3u7t6DRCq8C0kkbaEokVZPs0Vw82+WJ3GV373e7NbBKjMSLoJGUo8vhqgZ8QZZZuTffYpdfw8RBLXVhfuMRIEMM8IEEnm6CCbHMEcCiZcRqd9M9DMNiRQkwkYiBYmwkUhBImzQiKhzGsePHcj7XNhKfACdsFzITDEu3QAAAABJRU5ErkJggg==";

	private const string B64_Local = "iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAAsTAAALEwEAmpwYAAACyUlEQVR4nO2ZS2xNQRjHf9JKpK1HvEOkEiUW9boogliVJYnYCvFKY2NHCLEjSKkNEUSIjTYsrDwW4pWIepUiHiUV0g1KUFFUvuR/k9n03nOOe+9Mub9kkpN7vv/M/5w7Z+abGSjy75ECDgAPgU8qdr0fmEHADAGOA7+Bnl6K3TsGDCYwRgH3ZPIbUA/UAINUavQPdCnmLjCSQOgP3JCxVmBChtgq4LFirwOlBMAOGWoDhkeIt5hX0mzFM9aXO2VmQQzdQmk+qIt5Y4OMXEigvSTtOjxyTiZWJdCukbYJj7yUCfs44zJJ2hd45ItMlCXQlktrdXh/ADMTlwppP+ORPt+FzsrE6gTatdI24pH1MnExgfaytPYg3rBJ6KOM2OQUlUXSvAcG4pltMmPpwYgI8RbzWpotBECpErMeJWoTs3y4TxR7DSghECw1viNjXVrQzFGuZGUu0AB8V0xzxH+r4N/D0SwLml/AkRD6fSama/HS4iwpW7TImZZRWeQ/pgSYCWxSemxr3nbgq0q7fmtSTCqUEWgMsBl4k+HD7a10ALuBSh/GRwMngZ+OoWcahVbqDY9Tml2m65TuWcxzR9cNnCjULkU/YKOTPti4fgqYl6Cu+cBpZ26w9XGd2sgLA9Rg+s2dB8bnoN6xwBmn3saEC6Ssfb3ZeVNLc90AsEx1Wxu31E1zNsM+cBYfk8kfVcBTtfVI25V/veuWztttNh1K/hkm8+ntGvOQmHpV9FYjSaGoBN6p7b1JK5mlYfKHrgvNbA2x3Um25Uuc9HgX/tgjD7fjztwrnA3bnA9pMSh3NoKXxxFelcj2Pn1TJy9XogqmStCpzSffVDg74FOiCPYp+CDh0CBP5i0rNxVcSzjUypOdBEV+gMWEwxLnWCorOxVsh3bV+KcauC9P26PmPumDuJBKa5zjKMtHDmvV5Nt4B3CoQHlYkSL0Nf4AU7gii/wb2wwAAAAASUVORK5CYII=";

	private const string B64_Misc = "iVBORw0KGgoAAAANSUhEUgAAADIAAAAyCAYAAAAeP4ixAAAACXBIWXMAAAsTAAALEwEAmpwYAAABdklEQVR4nO2YUW6DMAyG/yduMLSzrHegp+porzWUC5SHXgB2jUyTghTRJE1iDK7kX0JqbMvkg8QuAVQqVak6ADMAS7xml4s7b1Rb3Gy5ph3yRrUEU7XOw5U3K9AAGAIxOXZRIAOAn0BMjl0UCEViQUzhMhMLMhQuM7EgpRIFYrRqQRYIRWJBjFYtYSClEgVitGpBFghFh4NMXvBbf1h1G8FMq0/SM1Ne1duo2/C04385LToz5Y1KT1Fe5PHHHwDubvwA8Flgt7nz8wMbADcAv+6J9s6GDH8MpAUwut+jG6PAbmtAroHX+u3FpvwxEApEWwuyrOsvACdvsyHDHwOhQIAKcvImGtrAIX/qjdRCtLUgfWDpXLzYlD+1R2ohRspm792Tn9wk15s95k9VLSqELQWhSFTVokhU1WoY+sghVevK1Ed2r1ozQx85pGrNDH1kCwgrpY/sXrUahj5ySNWiKFW1Sv+6Pzx79vz0FAXPN9NTFJUKMvQHjBgyjDhawHEAAAAASUVORK5CYII=";

	private const string B64_Cursor = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAACXBIWXMAAAsTAAALEwEAmpwYAAABC0lEQVR4nLXWv0oDQRAG8PVPoU1uZ8AuoEVqH8E8QeoUVr5DrLe5maSxsBR8grOV2wkieYboK9gJqU3jyh4IIoHs7d0MbPv9YHc+WGN2TQgHRnPA89P5yp2oASgcwNMrvCwKNaA5nt+xXgz1AOEAwh+2pks1AJtDG7ucXykCHJEvEJ4qAhzQ0zcIz/QA+X0XujfOHaoB2Kxxh66kANilK6kA5nalFSAZXWkLYNuu5AGc3pV8gNO6khG8jY8dVxc8lVb4eljdnWYC9AbCj+DLW7ssJ2fCI1NVR0l3vxfw9GBW7rhVWCKwReGbzsG7Afq0NY17C/8HrAs/v+g1/A/wjLUb9B4eJ65aX1+XH02uuT9AKd6vAAAAAElFTkSuQmCC";

	private const string B64_Settings = "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAACXBIWXMAAAsTAAALEwEAmpwYAAAFZklEQVR4nO2ae4jVRRTHP6ttJrtFablm6FoqBgn2gGoLKyF7kEFKbEFE7/pDKOhlRqxWuLGV9Nik+qO/coX1r9wiJdz+EI3K8r989LLU0gKxXFd3tXVj4FwYDjPze97b7XfvFwbX+zszc77zOOfMmYE66qijjvLic2A0ZumnYGgADicYgL+kTmEwMwH5UplBgXBXigFop0B4VZHrSilTGAPYHmOVbKLABnBGDDtxuAiG8FzglZjEXAPVCUykijAe2Ag8LzPmg1H6DeBoQh/f75AfAFYB5wTqzRSdNoqOZcNypdx3wApgliVzI7AvYN1Dxk0bQrscAG6zZKcCTwBbgFOWXEe5yE8FBgMKfgWsVcrY5QjwIXBJoI85wBqZdVcbpu0e4MtAP4Oia+7oSeHLTfkJeBBoStCXkX0Y+Dlln0bXXHGtY8R9M1Aqx4HngMYM/TbK3h6K6Mulm9E5F4wBtqkOtgNTgCXAZmBEff8VmJuXAsBlwF7Vx4j0vUR02a6+bxPdM+MBx+jOUzIXiEHaCnwLtJA/JgvJrdKX6dPGPMdKMLpnwllife1GeyPqlDN4iWq71+E5DIfU6FINHgNaM7R3uoTBxlvskjhhUAxlj3zLYjNaRce4bjeICx3GxzS+OGV7i4VolAX/BViYoQ89AEPCJTEagQ8cChrj83iCdsZEBDi+0pliO9nyE8Qu3JNxVbHU4/LeimllX0vpy015iSrB/cAJh4LrgDMiluSoY0mawbtKAp4m+bsbGHZ4nAVUCW4C/nYQ6goYPL3n90fEB5eKjF1nVwJ/3iBLP9OSD8Eo/6dS8FGPbLtj5ufGDHr0Srg1IH+drKjdqt4fEig9m3dc8rVSrs0jt9ZhM+LiHeXLz3fIXJEg1W48w7K8VsYe1fgkj9xuJXdlgj6uljr/APMd3x9xrJI45RNywFHV6DiP3BEl15ygjzOlzmrHt2ciSOp+S8VMyDRpw6dzVQ3AMTnk2LhZVoUm1yd24mzPttghZwkkV7ATmE5OW6Al5hYwri4u2hyz3+wwwGaQb5fv42SJh8hPtvR6j5yM4DUxEyhvJ+jDkL9B/fa0au8kcL3l/nojyLfI/22vZFZMImR1g8bPR+FysfynWb81OOKDjoiAK0S+VO4udyD0o5LdHzEIhvxvwAb1+8WqnQE1e+tTkDfl3XKHwoscdYYl7G2Tfd1s7fmSa3tftfOQauMj9X2Hx+WGyI9KRjkSyz2HoVUxQ1SdU4hTnlRtLFPfX46wSyvFNoXIj0rS1Ys8j8NJBmGd2v8GLyiZFSkHuU+554OVTIjcAfyQgvxY4OOIsHpKjAcYfXK7ZK9m4xKDyDslZlbVnXL5sVOM2aCc+N70kHfdR7zoaNu4zUMe8j1ilBeq3z+r5qToWA95E4id56nTIjarX26PzEDfYn3/NGIr5ZYWL7mgcpBPG8K6PFLomi7TxcheOddXC/n5jrPJpnJfjQ2J9TZ7778iP0Fcpj5AnZCL2Ipcju6RcLm5jOQniWHsln87JTr03WY/Rs7X46fE6PQEVsaAGKVZOZOfLncJcSbDrISnyIAO1eA3knOzlVwQeCDh8/OVIH8wjwzz+JhPZEzA8bp66LDZYQ8qQX6fhNZJ3ifkhokSlx9y5A5D5FtTkl8vFzlL5VHGbKoADcC9DteaN/k1Mqj/C6ysZfKzJZ2lX5RE3eBO87wZ6nUY1qrGakXgQK3MfAnfJ8zLFYo8jgcMJlytGfI4EhcXUfA9r/GFI7dYEzPvu9sbkRTXHDlv3CdhayHJI6dCnWWKKoUhbz9qGK5V8vYg/B4gflIeSeXy3LVa0SQntS1yYDouGeJuuQ6row4qg38BdvA1gGZt2XIAAAAASUVORK5CYII=";

	public static void Init()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Expected O, but got Unknown
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Expected O, but got Unknown
		if ((UnityEngine.Object)(object)WhiteTexture == (UnityEngine.Object)null)
		{
			WhiteTexture = GeneratePixelTexture(Color.white);
		}
		if ((UnityEngine.Object)(object)RoundedTexture == (UnityEngine.Object)null)
		{
			RoundedTexture = GenerateRoundedTexture(32, 8);
		}
		if ((UnityEngine.Object)(object)CircleTexture == (UnityEngine.Object)null)
		{
			CircleTexture = GenerateCircleTexture(32);
		}
		if ((UnityEngine.Object)(object)ShadowTexture == (UnityEngine.Object)null)
		{
			ShadowTexture = GenerateShadowTexture(64);
		}
		if ((UnityEngine.Object)(object)HueTexture == (UnityEngine.Object)null)
		{
			HueTexture = GenerateHueTexture(128, 128, 10);
		}
		if ((UnityEngine.Object)(object)SatValTexture == (UnityEngine.Object)null)
		{
			SatValTexture = GenerateSatValTexture(128, 128);
		}
		if ((UnityEngine.Object)(object)IconLoot == (UnityEngine.Object)null)
		{
			IconLoot = LoadAndTint("iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAAsTAAALEwEAmpwYAAADK0lEQVR4nO2YS0hWQRTHf1b2JLKU3m+L2peRJURRi4Je1KZFqwxXvRZlBBViEESLwKAWBi2iF7mLjN5IRFAkvZUsKsmKCssSSyRjYILbYb7vzh3nm0/CPwzcxfn/z//cO/fcMxf68P+hBDgK1AMtej0AqoCF9GIUAreA7ph1U8dmDU0WJl1XU4gCajJYQE2IAvZZGKkFlgNj9VoBXLbg7Q9RwOo0Bn4D5Wm45TomFX9NiAKmiqStQI6DjuK0Ci2lnXGYEk/xcCO+Ot4IJ9SJ5Ks8bMU6AqJKJN/roRlUERClIvkFD+24lIAoEslfePggFhEQQ4Aujx+wLmAogfHMYwHPQ5vP1ROmrwLqtWYQ9AfOeDTfrVdNiCJSmf8MLEigMxf4ELqIfsB5Q9JGYLqDnuI0GvTO6lzecciQ7CFQ0APNAq0hdQ/iGRsNSVQXGu1Be4zuQlFtNbFuwBNmAN9FgvfApBjeMqBam/sG/ASa9bFzsYidrDWjOdoct+Y/GADcEcLKSHEajnoqV2M6zkoDr1hrR+Nu68bhjO2G5GVp4vNTvJhypTrUlxlit7qaHwV8EWKXYmb2cwYDHfqJnNTHyndp7mqOPo7KFj3SpYAjQkgdYiakiZ9lMH8FGC/i4lrkRMOB6XBS83lAuxDZHcM5IOKfAoNwwx6hpZrIiCQCO4TAJ2B4iljbUSEJhgEfBX9LEoEGQd6ZJjYTBSjsMjxRKxQK4i/Lr230fKCue4p8nTvqZZoNcbMgXbRMGP3foxL7QK3wssmGdFqQtsXEZ2oLmd7FU1jgriCp3+XZKmCR0FBTQSxeCZL6t2mDasFr9HDOHSc0X9qQfgiSbS9fYrjr6mfVTBGXpKjBQk95i4X8gCkRW1w3FKE60mM9TjzSJnIdC2i3Ib1x3EJ/x4C3Fu/DbMct9NqGdF+Q1NYgYRHXYgpYa6m1VPDu2ZCOC1IFbijRQ9gN4Ik+OqrrE8A8S41K4eWYDWm9IDWH/GcTwUA9eke9rMNyEm3z9BR6gkrhoS3JRCpH496wKpJUn2c4ZGdztWhPiTDH8E3IxuoA5uMI9dJ0ZtF8p+2L2weyhD/xtMbE/Fm2bAAAAABJRU5ErkJggg==");
		}
		if ((UnityEngine.Object)(object)IconEnemies == (UnityEngine.Object)null)
		{
			IconEnemies = LoadAndTint("iVBORw0KGgoAAAANSUhEUgAAADIAAAAyCAYAAAAeP4ixAAAACXBIWXMAAAsTAAALEwEAmpwYAAAA00lEQVR4nO2WQQ7DMAgE+f+zGuU7/cL24lwsuU3tBeN4R+IQWQZNAhFmYj1OM8PkOBgiSBLD0BLNro+dRNBZzDN3V6J3iSVFQCzyLRcksvOwewKJ7CaCjrVi5M4wjxe56F3u7t6DRCq8C0kkbaEokVZPs0Vw82+WJ3GV373e7NbBKjMSLoJGUo8vhqgZ8QZZZuTffYpdfw8RBLXVhfuMRIEMM8IEEnm6CCbHMEcCiZcRqd9M9DMNiRQkwkYiBYmwkUhBImzQiKhzGsePHcj7XNhKfACdsFzITDEu3QAAAABJRU5ErkJggg==");
		}
		if ((UnityEngine.Object)(object)IconLocal == (UnityEngine.Object)null)
		{
			IconLocal = LoadAndTint("iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAAsTAAALEwEAmpwYAAACyUlEQVR4nO2ZS2xNQRjHf9JKpK1HvEOkEiUW9boogliVJYnYCvFKY2NHCLEjSKkNEUSIjTYsrDwW4pWIepUiHiUV0g1KUFFUvuR/k9n03nOOe+9Mub9kkpN7vv/M/5w7Z+abGSjy75ECDgAPgU8qdr0fmEHADAGOA7+Bnl6K3TsGDCYwRgH3ZPIbUA/UAINUavQPdCnmLjCSQOgP3JCxVmBChtgq4LFirwOlBMAOGWoDhkeIt5hX0mzFM9aXO2VmQQzdQmk+qIt5Y4OMXEigvSTtOjxyTiZWJdCukbYJj7yUCfs44zJJ2hd45ItMlCXQlktrdXh/ADMTlwppP+ORPt+FzsrE6gTatdI24pH1MnExgfaytPYg3rBJ6KOM2OQUlUXSvAcG4pltMmPpwYgI8RbzWpotBECpErMeJWoTs3y4TxR7DSghECw1viNjXVrQzFGuZGUu0AB8V0xzxH+r4N/D0SwLml/AkRD6fSama/HS4iwpW7TImZZRWeQ/pgSYCWxSemxr3nbgq0q7fmtSTCqUEWgMsBl4k+HD7a10ALuBSh/GRwMngZ+OoWcahVbqDY9Tml2m65TuWcxzR9cNnCjULkU/YKOTPti4fgqYl6Cu+cBpZ26w9XGd2sgLA9Rg+s2dB8bnoN6xwBmn3saEC6Ssfb3ZeVNLc90AsEx1Wxu31E1zNsM+cBYfk8kfVcBTtfVI25V/veuWztttNh1K/hkm8+ntGvOQmHpV9FYjSaGoBN6p7b1JK5mlYfKHrgvNbA2x3Um25Uuc9HgX/tgjD7fjztwrnA3bnA9pMSh3NoKXxxFelcj2Pn1TJy9XogqmStCpzSffVDg74FOiCPYp+CDh0CBP5i0rNxVcSzjUypOdBEV+gMWEwxLnWCorOxVsh3bV+KcauC9P26PmPumDuJBKa5zjKMtHDmvV5Nt4B3CoQHlYkSL0Nf4AU7gii/wb2wwAAAAASUVORK5CYII=");
		}
		if ((UnityEngine.Object)(object)IconMisc == (UnityEngine.Object)null)
		{
			IconMisc = LoadAndTint("iVBORw0KGgoAAAANSUhEUgAAADIAAAAyCAYAAAAeP4ixAAAACXBIWXMAAAsTAAALEwEAmpwYAAABdklEQVR4nO2YUW6DMAyG/yduMLSzrHegp+porzWUC5SHXgB2jUyTghTRJE1iDK7kX0JqbMvkg8QuAVQqVak6ADMAS7xml4s7b1Rb3Gy5ph3yRrUEU7XOw5U3K9AAGAIxOXZRIAOAn0BMjl0UCEViQUzhMhMLMhQuM7EgpRIFYrRqQRYIRWJBjFYtYSClEgVitGpBFghFh4NMXvBbf1h1G8FMq0/SM1Ne1duo2/C04385LToz5Y1KT1Fe5PHHHwDubvwA8Flgt7nz8wMbADcAv+6J9s6GDH8MpAUwut+jG6PAbmtAroHX+u3FpvwxEApEWwuyrOsvACdvsyHDHwOhQIAKcvImGtrAIX/qjdRCtLUgfWDpXLzYlD+1R2ohRspm792Tn9wk15s95k9VLSqELQWhSFTVokhU1WoY+sghVevK1Ed2r1ozQx85pGrNDH1kCwgrpY/sXrUahj5ySNWiKFW1Sv+6Pzx79vz0FAXPN9NTFJUKMvQHjBgyjDhawHEAAAAASUVORK5CYII=");
		}
		if ((UnityEngine.Object)(object)IconSettings == (UnityEngine.Object)null)
		{
			IconSettings = LoadAndTint("iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAACXBIWXMAAAsTAAALEwEAmpwYAAAFZklEQVR4nO2ae4jVRRTHP6ttJrtFablm6FoqBgn2gGoLKyF7kEFKbEFE7/pDKOhlRqxWuLGV9Nik+qO/coX1r9wiJdz+EI3K8r989LLU0gKxXFd3tXVj4FwYDjPze97b7XfvFwbX+zszc77zOOfMmYE66qijjvLic2A0ZumnYGgADicYgL+kTmEwMwH5UplBgXBXigFop0B4VZHrSilTGAPYHmOVbKLABnBGDDtxuAiG8FzglZjEXAPVCUykijAe2Ag8LzPmg1H6DeBoQh/f75AfAFYB5wTqzRSdNoqOZcNypdx3wApgliVzI7AvYN1Dxk0bQrscAG6zZKcCTwBbgFOWXEe5yE8FBgMKfgWsVcrY5QjwIXBJoI85wBqZdVcbpu0e4MtAP4Oia+7oSeHLTfkJeBBoStCXkX0Y+Dlln0bXXHGtY8R9M1Aqx4HngMYM/TbK3h6K6Mulm9E5F4wBtqkOtgNTgCXAZmBEff8VmJuXAsBlwF7Vx4j0vUR02a6+bxPdM+MBx+jOUzIXiEHaCnwLtJA/JgvJrdKX6dPGPMdKMLpnwllife1GeyPqlDN4iWq71+E5DIfU6FINHgNaM7R3uoTBxlvskjhhUAxlj3zLYjNaRce4bjeICx3GxzS+OGV7i4VolAX/BViYoQ89AEPCJTEagQ8cChrj83iCdsZEBDi+0pliO9nyE8Qu3JNxVbHU4/LeimllX0vpy015iSrB/cAJh4LrgDMiluSoY0mawbtKAp4m+bsbGHZ4nAVUCW4C/nYQ6goYPL3n90fEB5eKjF1nVwJ/3iBLP9OSD8Eo/6dS8FGPbLtj5ufGDHr0Srg1IH+drKjdqt4fEig9m3dc8rVSrs0jt9ZhM+LiHeXLz3fIXJEg1W48w7K8VsYe1fgkj9xuJXdlgj6uljr/APMd3x9xrJI45RNywFHV6DiP3BEl15ygjzOlzmrHt2ciSOp+S8VMyDRpw6dzVQ3AMTnk2LhZVoUm1yd24mzPttghZwkkV7ATmE5OW6Al5hYwri4u2hyz3+wwwGaQb5fv42SJh8hPtvR6j5yM4DUxEyhvJ+jDkL9B/fa0au8kcL3l/nojyLfI/22vZFZMImR1g8bPR+FysfynWb81OOKDjoiAK0S+VO4udyD0o5LdHzEIhvxvwAb1+8WqnQE1e+tTkDfl3XKHwoscdYYl7G2Tfd1s7fmSa3tftfOQauMj9X2Hx+WGyI9KRjkSyz2HoVUxQ1SdU4hTnlRtLFPfX46wSyvFNoXIj0rS1Ys8j8NJBmGd2v8GLyiZFSkHuU+554OVTIjcAfyQgvxY4OOIsHpKjAcYfXK7ZK9m4xKDyDslZlbVnXL5sVOM2aCc+N70kHfdR7zoaNu4zUMe8j1ilBeq3z+r5qToWA95E4id56nTIjarX26PzEDfYn3/NGIr5ZYWL7mgcpBPG8K6PFLomi7TxcheOddXC/n5jrPJpnJfjQ2J9TZ7778iP0Fcpj5AnZCL2Ipcju6RcLm5jOQniWHsln87JTr03WY/Rs7X46fE6PQEVsaAGKVZOZOfLncJcSbDrISnyIAO1eA3knOzlVwQeCDh8/OVIH8wjwzz+JhPZEzA8bp66LDZYQ8qQX6fhNZJ3ifkhokSlx9y5A5D5FtTkl8vFzlL5VHGbKoADcC9DteaN/k1Mqj/C6ysZfKzJZ2lX5RE3eBO87wZ6nUY1qrGakXgQK3MfAnfJ8zLFYo8jgcMJlytGfI4EhcXUfA9r/GFI7dYEzPvu9sbkRTXHDlv3CdhayHJI6dCnWWKKoUhbz9qGK5V8vYg/B4gflIeSeXy3LVa0SQntS1yYDouGeJuuQ6row4qg38BdvA1gGZt2XIAAAAASUVORK5CYII=");
		}
		if ((UnityEngine.Object)(object)CursorTexture == (UnityEngine.Object)null)
		{
			CursorTexture = LoadBase64("iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAACXBIWXMAAAsTAAALEwEAmpwYAAABC0lEQVR4nLXWv0oDQRAG8PVPoU1uZ8AuoEVqH8E8QeoUVr5DrLe5maSxsBR8grOV2wkieYboK9gJqU3jyh4IIoHs7d0MbPv9YHc+WGN2TQgHRnPA89P5yp2oASgcwNMrvCwKNaA5nt+xXgz1AOEAwh+2pks1AJtDG7ucXykCHJEvEJ4qAhzQ0zcIz/QA+X0XujfOHaoB2Kxxh66kANilK6kA5nalFSAZXWkLYNuu5AGc3pV8gNO6khG8jY8dVxc8lVb4eljdnWYC9AbCj+DLW7ssJ2fCI1NVR0l3vxfw9GBW7rhVWCKwReGbzsG7Afq0NY17C/8HrAs/v+g1/A/wjLUb9B4eJ65aX1+XH02uuT9AKd6vAAAAAElFTkSuQmCC");
		}
		RoundedBoxStyle = new GUIStyle();
		RoundedBoxStyle.normal.background = RoundedTexture;
		RoundedBoxStyle.border = new RectOffset(8, 8, 8, 8);
	}

	private static Texture2D LoadBase64(string b64)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		byte[] array = Convert.FromBase64String(b64);
		Texture2D val = new Texture2D(1, 1);
		ImageConversion.LoadImage(val, array);
		val.Apply();
		return val;
	}

	private static Texture2D LoadAndTint(string b64)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = LoadBase64(b64);
		Color[] pixels = val.GetPixels();
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);
		}
		val.SetPixels(pixels);
		val.Apply();
		return val;
	}

	private static Color GetColor(int hex)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)((hex >> 16) & 0xFF) / 255f;
		float num2 = (float)((hex >> 8) & 0xFF) / 255f;
		float num3 = (float)(hex & 0xFF) / 255f;
		return new Color(num, num2, num3, 1f);
	}

	private static Texture2D GeneratePixelTexture(Color color)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected O, but got Unknown
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(1, 1);
		val.SetPixel(0, 0, color);
		val.Apply();
		return val;
	}

	private static Texture2D GenerateRoundedTexture(int width, int radius)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(width, width);
		Color[] array = (Color[])(object)new Color[width * width];
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < width; j++)
			{
				bool flag = false;
				float num = 0f;
				if (j < radius && i < radius)
				{
					num = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2((float)radius - 0.5f, (float)radius - 0.5f));
					flag = true;
				}
				else if (j > width - radius - 1 && i < radius)
				{
					num = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2((float)(width - radius) - 0.5f, (float)radius - 0.5f));
					flag = true;
				}
				else if (j < radius && i > width - radius - 1)
				{
					num = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2((float)radius - 0.5f, (float)(width - radius) - 0.5f));
					flag = true;
				}
				else if (j > width - radius - 1 && i > width - radius - 1)
				{
					num = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2((float)(width - radius) - 0.5f, (float)(width - radius) - 0.5f));
					flag = true;
				}
				if (flag)
				{
					float num2 = Mathf.Clamp01((float)radius - num);
					array[i * width + j] = new Color(1f, 1f, 1f, num2);
				}
				else
				{
					array[i * width + j] = Color.white;
				}
			}
		}
		val.SetPixels(array);
		val.Apply();
		return val;
	}

	private static Texture2D GenerateCircleTexture(int size)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(size, size);
		Color[] array = (Color[])(object)new Color[size * size];
		float num = (float)size / 2f;
		float num2 = (float)size / 2f;
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float num3 = Vector2.Distance(new Vector2((float)j + 0.5f, (float)i + 0.5f), new Vector2(num, num));
				float num4 = Mathf.Clamp01(num2 - num3);
				array[i * size + j] = new Color(1f, 1f, 1f, num4);
			}
		}
		val.SetPixels(array);
		val.Apply();
		return val;
	}

	private static Texture2D GenerateShadowTexture(int size)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(size, size);
		Color[] array = (Color[])(object)new Color[size * size];
		float num = (float)size / 2f;
		float num2 = (float)size / 2f;
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float num3 = Vector2.Distance(new Vector2((float)j + 0.5f, (float)i + 0.5f), new Vector2(num, num));
				float num4 = Mathf.Clamp01(num3 / num2);
				float num5 = (1f - num4) * (1f - num4);
				array[i * size + j] = new Color(0f, 0f, 0f, num5);
			}
		}
		val.SetPixels(array);
		val.Apply();
		return val;
	}

	private static Texture2D GenerateHueTexture(int width, int height, int thickness)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(width, height);
		Color[] array = (Color[])(object)new Color[width * height];
		Vector2 val2 = default(Vector2);
		val2 = new Vector2((float)width / 2f, (float)height / 2f);
		float num = (float)width / 2f;
		float num2 = num - (float)thickness;
		Vector2 val3 = default(Vector2);
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				val3 = new Vector2((float)j, (float)i);
				float num3 = Vector2.Distance(val3, val2);
				if (num3 <= num && num3 >= num2)
				{
					float num4 = Mathf.Atan2(val3.y - val2.y, val3.x - val2.x) * 57.29578f;
					if (num4 < 0f)
					{
						num4 += 360f;
					}
					float num5 = num4 / 360f;
					array[i * width + j] = Color.HSVToRGB(num5, 1f, 1f);
				}
				else
				{
					array[i * width + j] = Color.clear;
				}
			}
		}
		val.SetPixels(array);
		val.Apply();
		return val;
	}

	private static Texture2D GenerateSatValTexture(int width, int height)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(width, height);
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				array[i * width + j] = Color.white;
			}
		}
		val.SetPixels(array);
		val.Apply();
		return val;
	}
}
