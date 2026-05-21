using UnityEngine;

/// <summary>
/// 关卡数据类
/// 存储三消游戏关卡的配置信息
/// </summary>
[CreateAssetMenu]
public class LevelData : ScriptableObject
{
    /// <summary>
    /// 棋盘宽度
    /// </summary>
    public int Width;
    
    /// <summary>
    /// 棋盘高度
    /// </summary>
    public int Height;
    
    /// <summary>
    /// 棋子类型数量
    /// </summary>
    public int Count;
    
    /// <summary>
    /// 棋子类型数组（按行优先顺序排列，0表示空位）
    /// </summary>
    public int[] Types;
}
