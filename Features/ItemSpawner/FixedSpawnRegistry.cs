using System;
using System.Collections.Generic;

namespace Cheat.Features.ItemSpawner;

internal static class FixedSpawnRegistry
{
	private static ItemSpawner.SpawnableItemDef CreateDef(string name, string resourcePath)
	{
		return new ItemSpawner.SpawnableItemDef
		{
			Name = SanitizeDisplayName(name, resourcePath),
			NativeId = resourcePath,
			ResourcePath = resourcePath
		};
	}

	private static string SanitizeDisplayName(string name, string resourcePath)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return name;
		}
		string text = name.Trim();
		if (!string.IsNullOrWhiteSpace(resourcePath) && resourcePath.StartsWith("Valuables/", StringComparison.OrdinalIgnoreCase))
		{
			if (text.StartsWith("珍贵的", StringComparison.Ordinal))
			{
				text = text.Substring("珍贵的".Length).TrimStart();
			}
			else if (text.StartsWith("贵重", StringComparison.Ordinal))
			{
				text = text.Substring("贵重".Length).TrimStart();
			}
			text = StripAreaPrefix(text);
		}
		return text;
	}

	private static string StripAreaPrefix(string value)
	{
		string[] array = new string[4] { "北极", "庄园", "博物馆", "巫师" };
		foreach (string text in array)
		{
			if (value.StartsWith(text, StringComparison.Ordinal))
			{
				return value.Substring(text.Length).TrimStart();
			}
		}
		return value;
	}

	public static List<ItemSpawner.SpawnableItemDef> BuildValuableDefs()
	{
		return new List<ItemSpawner.SpawnableItemDef>
		{
			CreateDef("贵重北极门禁卡", "Valuables/Valuable Arctic Keycard"),
			CreateDef("珍贵的北极橡皮擦", "Valuables/Valuable Arctic Eraser"),
			CreateDef("珍贵的北极风扇", "Valuables/Valuable Arctic Fan"),
			CreateDef("珍贵的北极火焰喷射器", "Valuables/Valuable Arctic Flamethrower"),
			CreateDef("珍贵的北极药剂", "Valuables/Valuable Arctic Pills"),
			CreateDef("珍贵的北极铀培养皿", "Valuables/Valuable Arctic Uranium Petri Dish"),
			CreateDef("珍贵的庄园乐器", "Valuables/Valuable Manor Instrument"),
			CreateDef("珍贵的庄园电视机", "Valuables/Valuable Manor Television"),
			CreateDef("珍贵的博物馆搅拌机", "Valuables/Valuable Museum Blender"),
			CreateDef("珍贵的博物馆金鱼标本", "Valuables/Valuable Museum GoldFish"),
			CreateDef("珍贵的博物馆牙齿机器人", "Valuables/Valuable Museum Teeth Bot"),
			CreateDef("珍贵的北极灭火器", "Valuables/Valuable Arctic Fire Extinguisher"),
			CreateDef("珍贵的北极生物腿骨", "Valuables/Valuable Arctic Creature Leg"),
			CreateDef("珍贵的北极冷冻舱", "Valuables/Valuable Arctic Cryo Pod"),
			CreateDef("珍贵的北极手电筒", "Valuables/Valuable Arctic Flashlight"),
			CreateDef("珍贵的北极吉他", "Valuables/Valuable Arctic Guitar"),
			CreateDef("珍贵的北极硬盘", "Valuables/Valuable Arctic HDD"),
			CreateDef("珍贵的北极重水", "Valuables/Valuable Arctic Heavy Water"),
			CreateDef("珍贵的北极冰块", "Valuables/Valuable Arctic Ice Block"),
			CreateDef("珍贵的北极冰锯", "Valuables/Valuable Arctic Ice Saw"),
			CreateDef("珍贵的北极冰镐", "Valuables/Valuable Arctic Icepick"),
			CreateDef("珍贵的北极凿岩机", "Valuables/Valuable Arctic Jackhammer"),
			CreateDef("珍贵的北极笔记本电脑", "Valuables/Valuable Arctic Laptop"),
			CreateDef("珍贵的北极电话", "Valuables/Valuable Arctic Phone"),
			CreateDef("珍贵的北极样品", "Valuables/Valuable Arctic Sample"),
			CreateDef("珍贵的北极样品冷藏箱", "Valuables/Valuable Arctic Sample Cooler"),
			CreateDef("珍贵的北极宽体样品冷藏箱", "Valuables/Valuable Arctic Sample Cooler Wide"),
			CreateDef("珍贵的北极样品六联包", "Valuables/Valuable Arctic Sample Six Pack"),
			CreateDef("珍贵的北极天平", "Valuables/Valuable Arctic Scale"),
			CreateDef("珍贵的北极科学站", "Valuables/Valuable Arctic Science Station"),
			CreateDef("珍贵的北极服务器机架", "Valuables/Valuable Arctic Server Rack"),
			CreateDef("珍贵的北极智能手表", "Valuables/Valuable Arctic Smartwatch"),
			CreateDef("珍贵的北极雪地摩托", "Valuables/Valuable Arctic Snow Bike"),
			CreateDef("珍贵的北极订书机", "Valuables/Valuable Arctic Stapler"),
			CreateDef("珍贵的北极录像带", "Valuables/Valuable Arctic VHS"),
			CreateDef("珍贵的庄园动物箱", "Valuables/Valuable Manor Animal Crate"),
			CreateDef("珍贵的庄园酒瓶", "Valuables/Valuable Manor Bottle"),
			CreateDef("珍贵的庄园小丑玩偶", "Valuables/Valuable Manor Clown"),
			CreateDef("珍贵的庄园棺材", "Valuables/Valuable Manor Coffin"),
			CreateDef("珍贵的庄园钻石陈列品", "Valuables/Valuable Manor Diamond Display"),
			CreateDef("珍贵的庄园恐龙摆件", "Valuables/Valuable Manor Dinosaur"),
			CreateDef("珍贵的庄园人偶", "Valuables/Valuable Manor Doll"),
			CreateDef("珍贵的庄园翡翠手镯", "Valuables/Valuable Manor Emerald Bracelet"),
			CreateDef("珍贵的庄园青蛙", "Valuables/Valuable Manor Frog"),
			CreateDef("珍贵的庄园地球仪", "Valuables/Valuable Manor Globe"),
			CreateDef("珍贵的庄园高脚杯", "Valuables/Valuable Manor Goblet"),
			CreateDef("珍贵的庄园黄金雕像", "Valuables/Valuable Manor Golden Statue"),
			CreateDef("珍贵的庄园留声机", "Valuables/Valuable Manor Gramophone"),
			CreateDef("珍贵的庄园落地钟", "Valuables/Valuable Manor Grandfather Clock"),
			CreateDef("珍贵的庄园竖琴", "Valuables/Valuable Manor Harp"),
			CreateDef("珍贵的庄园水壶", "Valuables/Valuable Manor Kettle"),
			CreateDef("珍贵的庄园放大镜", "Valuables/Valuable Manor Magnifying Glass"),
			CreateDef("珍贵的庄园地图", "Valuables/Valuable Manor Map"),
			CreateDef("珍贵的庄园钱币", "Valuables/Valuable Manor Money"),
			CreateDef("珍贵的庄园陶笛", "Valuables/Valuable Manor Ocarina"),
			CreateDef("珍贵的庄园音乐盒", "Valuables/Valuable Manor Music Box"),
			CreateDef("珍贵的庄园老式相机", "Valuables/Valuable Manor Old Camera"),
			CreateDef("珍贵的庄园油画", "Valuables/Valuable Manor Painting"),
			CreateDef("珍贵的庄园钢琴", "Valuables/Valuable Manor Piano"),
			CreateDef("珍贵的庄园怀表", "Valuables/Valuable Manor Pocket Watch"),
			CreateDef("珍贵的庄园收音机", "Valuables/Valuable Manor Radio"),
			CreateDef("珍贵的庄园尖叫人偶", "Valuables/Valuable Manor Scream Doll"),
			CreateDef("珍贵的庄园瓶中船", "Valuables/Valuable Manor Ship in a bottle"),
			CreateDef("珍贵的庄园望远镜", "Valuables/Valuable Manor Telescope"),
			CreateDef("珍贵的庄园玩具猴子", "Valuables/Valuable Manor Toy Monkey"),
			CreateDef("珍贵的庄园奖杯", "Valuables/Valuable Manor Trophy"),
			CreateDef("珍贵的庄园铀水杯", "Valuables/Valuable Manor Uranium Mug"),
			CreateDef("珍贵的庄园铀餐盘", "Valuables/Valuable Manor Uranium Plate"),
			CreateDef("珍贵的庄园花瓶", "Valuables/Valuable Manor Vase"),
			CreateDef("珍贵的庄园大型花瓶", "Valuables/Valuable Manor Vase Big"),
			CreateDef("珍贵的庄园厚重花瓶", "Valuables/Valuable Manor Vase Chunky"),
			CreateDef("珍贵的庄园小型花瓶", "Valuables/Valuable Manor Vase Small"),
			CreateDef("珍贵的博物馆婴儿头颅标本", "Valuables/Valuable Museum Baby Head"),
			CreateDef("珍贵的博物馆香蕉弓", "Valuables/Valuable Museum Banana Bow"),
			CreateDef("珍贵的博物馆便携音响", "Valuables/Valuable Museum Boombox"),
			CreateDef("珍贵的博物馆汽车模型", "Valuables/Valuable Museum Car"),
			CreateDef("珍贵的博物馆鸡尾酒摆件", "Valuables/Valuable Museum Cocktail"),
			CreateDef("珍贵的博物馆酷炫大脑标本", "Valuables/Valuable Museum Cool brain"),
			CreateDef("珍贵的博物馆立方球", "Valuables/Valuable Museum Cube ball"),
			CreateDef("珍贵的博物馆立方体雕塑", "Valuables/Valuable Museum Cubic Sculpture"),
			CreateDef("珍贵的博物馆立方塔", "Valuables/Valuable Museum Cubic Tower"),
			CreateDef("珍贵的博物馆鸭人标本", "Valuables/Valuable Museum duck man"),
			CreateDef("珍贵的博物馆神秘蛋", "Valuables/Valuable Museum Egg"),
			CreateDef("珍贵的博物馆鱼类标本", "Valuables/Valuable Museum Fish"),
			CreateDef("珍贵的博物馆肉块标本", "Valuables/Valuable Museum Flesh Blob"),
			CreateDef("珍贵的博物馆宝石汉堡", "Valuables/Valuable Museum Gem Burger"),
			CreateDef("珍贵的博物馆金色漩涡摆件", "Valuables/Valuable Museum Golden Swirl"),
			CreateDef("珍贵的博物馆金牙标本", "Valuables/Valuable Museum GoldTooth"),
			CreateDef("珍贵的博物馆口香糖球", "Valuables/Valuable Museum Gumball"),
			CreateDef("珍贵的博物馆手掌脸标本", "Valuables/Valuable Museum Handface"),
			CreateDef("珍贵的博物馆马匹标本", "Valuables/Valuable Museum Horse"),
			CreateDef("珍贵的博物馆瓢虫标本", "Valuables/Valuable Museum ladybug"),
			CreateDef("珍贵的博物馆牛奶瓶", "Valuables/Valuable Museum Milk"),
			CreateDef("珍贵的博物馆猴子盒", "Valuables/Valuable Museum MonkeyBox"),
			CreateDef("珍贵的博物馆奶嘴", "Valuables/Valuable Museum Pacifier"),
			CreateDef("珍贵的博物馆痘痘男标本", "Valuables/Valuable Museum PimpleGuy"),
			CreateDef("珍贵的博物馆飞机模型", "Valuables/Valuable Museum Plane"),
			CreateDef("珍贵的博物馆鲁本玩偶", "Valuables/Valuable Museum RubenDoll"),
			CreateDef("珍贵的博物馆蠹虫标本", "Valuables/Valuable Museum SilverFish"),
			CreateDef("珍贵的博物馆高个子标本", "Valuables/Valuable Museum Tall Guy"),
			CreateDef("珍贵的博物馆吐司摆件", "Valuables/Valuable Museum Toast"),
			CreateDef("珍贵的博物馆牙齿标本", "Valuables/Valuable Museum Tooth"),
			CreateDef("珍贵的北极咖啡杯", "Valuables/Valuable Arctic Coffee Cup"),
			CreateDef("珍贵的北极计算机", "Valuables/Valuable Arctic Computer"),
			CreateDef("珍贵的北极离心机", "Valuables/Valuable Arctic Centrifuge"),
			CreateDef("珍贵的北极照相机", "Valuables/Valuable Arctic Camera"),
			CreateDef("珍贵的北极盆栽", "Valuables/Valuable Arctic Bonsai"),
			CreateDef("珍贵的北极计算器", "Valuables/Valuable Arctic Calculator"),
			CreateDef("珍贵的北极大号样品", "Valuables/Valuable Arctic Big Sample"),
			CreateDef("珍贵的北极桶", "Valuables/Valuable Arctic Barrel"),
			CreateDef("珍贵的巫师炼金台", "Valuables/Valuable Wizard Alchemy Station"),
			CreateDef("珍贵的博物馆蠕虫标本", "Valuables/Valuable Museum Worm"),
			CreateDef("珍贵的博物馆铁丝人偶", "Valuables/Valuable Museum Wire Figure"),
			CreateDef("珍贵的博物馆黑胶唱片", "Valuables/Valuable Museum Vinyl"),
			CreateDef("珍贵的博物馆豪华铀水杯", "Valuables/Valuable Museum Uranium Mug Deluxe"),
			CreateDef("珍贵的博物馆托盘", "Valuables/Valuable Museum Tray"),
			CreateDef("珍贵的博物馆交通灯模型", "Valuables/Valuable Museum Traffic Light"),
			CreateDef("珍贵的巫师鸟类头骨", "Valuables/Valuable Wizard Bird Skull"),
			CreateDef("珍贵的巫师扫帚", "Valuables/Valuable Wizard Broom"),
			CreateDef("珍贵的巫师甲虫", "Valuables/Valuable Wizard Bug"),
			CreateDef("珍贵的巫师坩埚箱", "Valuables/Valuable Wizard Cauldron Box"),
			CreateDef("珍贵的巫师噬咬之书", "Valuables/Valuable Wizard Chomp Book"),
			CreateDef("珍贵的巫师王冠", "Valuables/Valuable Wizard Crown"),
			CreateDef("珍贵的巫师水晶", "Valuables/Valuable Wizard Crystal"),
			CreateDef("珍贵的巫师水晶球", "Valuables/Valuable Wizard Crystal Ball"),
			CreateDef("珍贵的巫师知识魔方", "Valuables/Valuable Wizard Cube of Knowledge"),
			CreateDef("珍贵的巫师钻石", "Valuables/Valuable Wizard Diamond"),
			CreateDef("珍贵的巫师巨龙头骨", "Valuables/Valuable Wizard Dragon Skull"),
			CreateDef("珍贵的巫师达姆高尔法杖", "Valuables/Valuable Wizard Dumgolfs Staff"),
			CreateDef("珍贵的巫师眼球", "Valuables/Valuable Wizard Eye Ball"),
			CreateDef("珍贵的巫师奥皮戈克斯之眼", "Valuables/Valuable Wizard Eye of Orpigox"),
			CreateDef("珍贵的巫师永恒蜡烛", "Valuables/Valuable Wizard Forever Candle"),
			CreateDef("珍贵的巫师命运卡牌", "Valuables/Valuable Wizard Fortune Card"),
			CreateDef("珍贵的巫师宝石盒", "Valuables/Valuable Wizard Gem Box"),
			CreateDef("珍贵的巫师发光罐", "Valuables/Valuable Wizard Glowing Jar"),
			CreateDef("珍贵的巫师哥布林手臂", "Valuables/Valuable Wizard Goblin Arm"),
			CreateDef("珍贵的巫师哥布林头颅", "Valuables/Valuable Wizard Goblin Head"),
			CreateDef("珍贵的巫师狮鹫雕像", "Valuables/Valuable Wizard Griffin Statue"),
			CreateDef("珍贵的巫师漂浮药剂", "Valuables/Valuable Wizard Levitation Potion"),
			CreateDef("珍贵的巫师爱情药剂", "Valuables/Valuable Wizard Love Potion"),
			CreateDef("珍贵的巫师大师药剂", "Valuables/Valuable Wizard Master Potion"),
			CreateDef("珍贵的巫师吊坠", "Valuables/Valuable Wizard Pendant"),
			CreateDef("珍贵的巫师毒圣杯", "Valuables/Valuable Wizard Poison Chalice"),
			CreateDef("珍贵的巫师能量水晶", "Valuables/Valuable Wizard Power Crystal"),
			CreateDef("珍贵的巫师红蘑菇", "Valuables/Valuable Wizard Red Mushroom"),
			CreateDef("珍贵的巫师小型宝石", "Valuables/Valuable Wizard Small Gem"),
			CreateDef("珍贵的巫师小型药剂", "Valuables/Valuable Wizard Small Potion"),
			CreateDef("珍贵的巫师蜘蛛药剂", "Valuables/Valuable Wizard Spider Potion"),
			CreateDef("珍贵的巫师星辰魔杖", "Valuables/Valuable Wizard Star Wand"),
			CreateDef("珍贵的巫师之剑", "Valuables/Valuable Wizard Sword"),
			CreateDef("珍贵的巫师触手", "Valuables/Valuable Wizard Tentacle"),
			CreateDef("珍贵的巫师时光沙漏", "Valuables/Valuable Wizard Time Glass"),
			CreateDef("珍贵的巫师巨魔手指", "Valuables/Valuable Wizard Troll Finger"),
			CreateDef("珍贵的巫师独角兽角", "Valuables/Valuable Wizard Unicorn Horn")
		};
	}

	public static List<ItemSpawner.SpawnableItemDef> BuildEquipmentDefs()
	{
		return new List<ItemSpawner.SpawnableItemDef>
		{
			CreateDef("自动拾取未锁定装备", "Items/Item Unequip Auto Hold"),
			CreateDef("物品生成日志", "Items/Item Spawn Logs"),
			CreateDef("物品抗性", "Items/Item Resist"),
			CreateDef("物品价值", "Items/Item Value"),
			CreateDef("物品抗性升级", "Items/Item Resist Upgrade"),
			CreateDef("物品价值升级", "Items/Item Value Upgrade"),
			CreateDef("物品召唤", "Items/Item Conjurer"),
			CreateDef("物品解困", "Items/Item Unstuck"),
			CreateDef("震荡波手榴弹", "Items/Item Grenade Shockwave"),
			CreateDef("人形手榴弹", "Items/Item Grenade Human"),
			CreateDef("高爆手榴弹", "Items/Item Grenade Explosive"),
			CreateDef("胶带捆绑手榴弹", "Items/Item Grenade Duct Taped"),
			CreateDef("物品提取追踪器", "Items/Item Extraction Tracker"),
			CreateDef("鸭子桶", "Items/Item Duck Bucket"),
			CreateDef("零重力无人机", "Items/Item Drone Zero Gravity"),
			CreateDef("扭矩无人机", "Items/Item Drone Torque"),
			CreateDef("无敌无人机", "Items/Item Drone Indestructible"),
			CreateDef("轻型无人机", "Items/Item Drone Feather"),
			CreateDef("电池无人机", "Items/Item Drone Battery"),
			CreateDef("小型购物车", "Items/Item Cart Small"),
			CreateDef("中型购物车", "Items/Item Cart Medium"),
			CreateDef("激光购物车", "Items/Item Cart Laser"),
			CreateDef("加农炮购物车", "Items/Item Cart Cannon"),
			CreateDef("物品数量限制", "Items/Item Limits"),
			CreateDef("眩晕手榴弹", "Items/Item Grenade Stun"),
			CreateDef("手枪", "Items/Item Gun Handgun"),
			CreateDef("激光枪", "Items/Item Gun Laser"),
			CreateDef("震荡波枪", "Items/Item Gun Shockwave"),
			CreateDef("霰弹枪", "Items/Item Gun Shotgun"),
			CreateDef("眩晕枪", "Items/Item Gun Stun"),
			CreateDef("麻醉枪", "Items/Item Gun Tranq"),
			CreateDef("大型医疗包", "Items/Item Health Pack Large"),
			CreateDef("中型医疗包", "Items/Item Health Pack Medium"),
			CreateDef("小型医疗包", "Items/Item Health Pack Small"),
			CreateDef("棒球棍（近战）", "Items/Item Melee Baseball Bat"),
			CreateDef("平底锅（近战）", "Items/Item Melee Frying Pan"),
			CreateDef("充气锤（近战）", "Items/Item Melee Inflatable Hammer"),
			CreateDef("大锤（近战）", "Items/Item Melee Sledge Hammer"),
			CreateDef("电击棍（近战）", "Items/Item Melee Stun Baton"),
			CreateDef("长剑（近战）", "Items/Item Melee Sword"),
			CreateDef("高爆地雷", "Items/Item Mine Explosive"),
			CreateDef("震荡波地雷", "Items/Item Mine Shockwave"),
			CreateDef("眩晕地雷", "Items/Item Mine Stun"),
			CreateDef("零重力球体", "Items/Item Orb Zero Gravity"),
			CreateDef("相位传送桥", "Items/Item Phase Bridge"),
			CreateDef("能量水晶", "Items/Item Power Crystal"),
			CreateDef("橡胶鸭", "Items/Item Rubber Duck"),
			CreateDef("死亡头颅电池升级件", "Items/Item Upgrade Death Head Battery"),
			CreateDef("地图玩家数升级件", "Items/Item Upgrade Map Player Count"),
			CreateDef("蹲伏恢复升级件", "Items/Item Upgrade Player Crouch Rest"),
			CreateDef("能量上限升级件", "Items/Item Upgrade Player Energy"),
			CreateDef("额外跳跃升级件", "Items/Item Upgrade Player Extra Jump"),
			CreateDef("抓取距离升级件", "Items/Item Upgrade Player Grab Range"),
			CreateDef("抓取力度升级件", "Items/Item Upgrade Player Grab Strength"),
			CreateDef("生命值上限升级件", "Items/Item Upgrade Player Health"),
			CreateDef("冲刺速度升级件", "Items/Item Upgrade Player Sprint Speed"),
			CreateDef("翻滚攀爬升级件", "Items/Item Upgrade Player Tumble Climb"),
			CreateDef("翻滚冲刺升级件", "Items/Item Upgrade Player Tumble Launch"),
			CreateDef("翻滚滑翔翼升级件", "Items/Item Upgrade Player Tumble Wings"),
			CreateDef("贵重物品追踪器", "Items/Item Valuable Tracker")
		};
	}
}
