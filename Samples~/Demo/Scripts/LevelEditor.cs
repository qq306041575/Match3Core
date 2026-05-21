using Match3Core;
using UnityEngine;

/// <summary>
/// 关卡编辑器
/// 用于在Unity编辑器中创建和编辑关卡数据
/// 支持鼠标左键切换棋子类型，右键清除棋子
/// </summary>
public class LevelEditor : MonoBehaviour
{
    /// <summary>
    /// 对象工厂（用于对象池管理）
    /// </summary>
    [SerializeField] ObjectFactory _factory;
    
    /// <summary>
    /// 棋盘宽度
    /// </summary>
    [SerializeField] int _width;
    
    /// <summary>
    /// 棋盘高度
    /// </summary>
    [SerializeField] int _height;
    
    /// <summary>
    /// 棋子类型数量
    /// </summary>
    [SerializeField] int _count;

#if UNITY_EDITOR
    /// <summary>
    /// 棋盘对象映射
    /// </summary>
    Transform[,] _map;
    
    /// <summary>
    /// 关卡数据对象
    /// </summary>
    LevelData _level;
    
    /// <summary>
    /// 棋子类型数组（按行优先顺序排列）
    /// </summary>
    int[] _types;

    void Awake()
    {
        _level = ScriptableObject.CreateInstance<LevelData>();
        UnityEditor.AssetDatabase.CreateAsset(_level, "Assets/LevelData.asset");
        _level.Width = _width;
        _level.Height = _height;
        _level.Count = _count;
        _types = new int[_width * _height];
        _map = new Transform[_height, _width];
        var board = new BoardGrid(_width, _height, _count);
        board.OnSetCell += OnSetCell;
        board.FillCell();
        SaveLevel();
    }

    void Update()
    {
        // 左键点击：切换棋子类型
        if (Input.GetMouseButtonDown(0))
        {
            if (HasCell(out int x, out int y))
            {
                var type = _types[y * _width + x];
                var obj = _map[y, x];
                if (obj != null)
                {
                    obj.localScale = Vector3.zero;
                    _factory.Recycle(type, obj);
                }
                if (type < _count)
                {
                    type += 1;
                }
                else
                {
                    type = 1;
                }
                OnSetCell(x, y, type);
                SaveLevel();
            }
        }
        // 右键点击：清除棋子
        else if (Input.GetMouseButtonDown(1))
        {
            if (HasCell(out int x, out int y))
            {
                var type = _types[y * _width + x];
                var obj = _map[y, x];
                if (obj != null)
                {
                    obj.localScale = Vector3.zero;
                    _factory.Recycle(type, obj);
                }
                _map[y, x] = null;
                _types[y * _width + x] = 0;
                SaveLevel();
            }
        }
    }

    /// <summary>
    /// 检查鼠标位置是否在棋盘范围内
    /// </summary>
    /// <param name="x">输出X坐标</param>
    /// <param name="y">输出Y坐标</param>
    /// <returns>是否在棋盘范围内</returns>
    bool HasCell(out int x, out int y)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var point = ray.origin - ray.direction * (ray.origin.z / ray.direction.z);
        x = Mathf.FloorToInt(point.x + _width * 0.5f);
        y = Mathf.FloorToInt(point.y + _height * 0.5f);
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    /// <summary>
    /// 保存关卡数据到文件
    /// </summary>
    void SaveLevel()
    {
        _level.Types = _types;
        UnityEditor.EditorUtility.SetDirty(_level);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("Level has saved in Assets/LevelData.asset");
    }

    /// <summary>
    /// 设置单元格对象
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="type">棋子类型</param>
    void OnSetCell(int x, int y, int type)
    {
        var obj = _factory.Get(type);
        obj.position = GetCellPosition(x, y);
        _map[y, x] = obj;
        _types[y * _width + x] = type;
    }

    /// <summary>
    /// 获取单元格的世界坐标
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>世界坐标</returns>
    Vector3 GetCellPosition(int x, int y)
    {
        return new Vector3(x - _width * 0.5f + 0.5f, y - _height * 0.5f + 0.5f, 0f);
    }
#endif
}
