using System.Collections;
using System.Collections.Generic;
using Match3Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏主控制器
/// 负责处理用户输入、游戏状态管理和UI更新
/// </summary>
public class Game : MonoBehaviour
{
    /// <summary>
    /// 对象工厂（用于对象池管理）
    /// </summary>
    [SerializeField] ObjectFactory _factory;
    
    /// <summary>
    /// 关卡数据
    /// </summary>
    [SerializeField] LevelData _level;
    
    /// <summary>
    /// 是否自动模式
    /// </summary>
    [SerializeField] bool _auto;
    
    /// <summary>
    /// 游戏结束文本
    /// </summary>
    [SerializeField] Text _textOver;
    
    /// <summary>
    /// 分数显示文本数组
    /// </summary>
    [SerializeField] Text[] _textScores;

    /// <summary>
    /// 各类型棋子得分统计
    /// </summary>
    Dictionary<int, int> _scores;
    
    /// <summary>
    /// 记录所有创建的棋子对象及其类型
    /// </summary>
    Dictionary<Transform, int> _records;
    
    /// <summary>
    /// 棋盘对象映射
    /// </summary>
    Transform[,] _map;
    
    /// <summary>
    /// 棋盘逻辑核心
    /// </summary>
    BoardGrid _board;
    
    /// <summary>
    /// 拖拽起始点
    /// </summary>
    Vector3 _dragPoint;
    
    /// <summary>
    /// 是否正在拖拽
    /// </summary>
    bool _isDragging;
    
    /// <summary>
    /// 是否正在处理动画
    /// </summary>
    bool _isBusy;
    
    /// <summary>
    /// 棋盘宽度
    /// </summary>
    int _width;
    
    /// <summary>
    /// 棋盘高度
    /// </summary>
    int _height;

    void Awake()
    {
        _scores = new Dictionary<int, int>();
        _records = new Dictionary<Transform, int>();
        _width = _level.Width;
        _height = _level.Height;
        _map = new Transform[_height, _width];
        _board = new BoardGrid(_width, _height, _level.Count);
        _board.OnSetExplosion += OnSetExplosion;
        _board.OnSetCell += OnSetCell;
        _board.OnSwapCell += OnSwapCell;
        _board.OnDropCell += OnDropCell;
        _board.OnNoMatch += OnNoMatch;
        _board.FillCell(_level.Types);
        // RecycleAll(true);
    }

    void Update()
    {
        if (_isBusy)
        {
            return;
        }
        
        // 自动模式
		if (_auto)
        {
            _board.SwapMatch();
        }
        // 处理鼠标拖拽开始
		else if (!_isDragging && Input.GetMouseButtonDown(0))
		{
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            _dragPoint = ray.origin - ray.direction * (ray.origin.z / ray.direction.z);
			_isDragging = true;
		}
        // 处理拖拽中
		else if (_isDragging && Input.GetMouseButton(0))
        {
            OnDrag();
        }
        // 拖拽结束
		else
		{
			_isDragging = false;
		}
    }

    /// <summary>
    /// 处理拖拽逻辑
    /// </summary>
    void OnDrag()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var p = ray.origin - ray.direction * (ray.origin.z / ray.direction.z) - _dragPoint;
        var x = Mathf.FloorToInt(_dragPoint.x + _width * 0.5f);
        var y = Mathf.FloorToInt(_dragPoint.y + _height * 0.5f);
            
        // 根据拖拽方向尝试交换棋子
        if (p.x > 0.5f && _board.SwapCell(x, y, x + 1, y))
        {
            _isDragging = false;
        }
        else if (p.x < -0.5f && _board.SwapCell(x, y, x - 1, y))
        {
            _isDragging = false;
        }
        else if (p.y > 0.5f && _board.SwapCell(x, y, x, y + 1))
        {
            _isDragging = false;
        }
        else if (p.y < -0.5f && _board.SwapCell(x, y, x, y - 1))
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// 设置爆炸对象
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="type">爆炸类型</param>
    void OnSetExplosion(int x, int y, int type)
    {
        var obj = _factory.Get(type);
        obj.name = "Explosion";
        obj.position = GetCellPosition(x, y);
        obj.SetParent(_map[y, x], true);
        _records.Add(obj, type);
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
        obj.SetParent(transform, true);
        _map[y, x] = obj;
        _records.Add(obj, type);
    }

    /// <summary>
    /// 处理棋子交换事件
    /// </summary>
    /// <param name="from">源单元格</param>
    /// <param name="to">目标单元格</param>
    /// <param name="matches">匹配列表</param>
    void OnSwapCell(BoardCell from, BoardCell to, List<BoardCell> matches)
    {
        StartCoroutine(SwapCell(from, to, matches));
    }

    /// <summary>
    /// 处理棋子下落事件
    /// </summary>
    /// <param name="offsets">下落偏移量列表</param>
    /// <param name="drops">下落单元格列表</param>
    /// <param name="matches">匹配列表</param>
    void OnDropCell(List<int> offsets, List<BoardCell> drops, List<BoardCell> matches)
    {
        StartCoroutine(DropCell(offsets, drops, matches));
    }

    /// <summary>
    /// 处理无解事件
    /// </summary>
    void OnNoMatch()
    {
        StartCoroutine(Replay());
    }

    /// <summary>
    /// 回收匹配的棋子并更新分数
    /// </summary>
    /// <param name="matches">匹配的单元格列表</param>
    void RecycleMatch(List<BoardCell> matches)
    {
        foreach (var cell in matches)
        {
            var obj = _map[cell.Y, cell.X];
            var explosion = obj.Find("Explosion");
            if (explosion != null && _records.ContainsKey(explosion))
            {
                explosion.localScale = Vector3.zero;
                explosion.SetParent(transform);
                _factory.Recycle(_records[explosion], explosion);
                _records.Remove(explosion);
            }
            _factory.Recycle(cell.Type, obj);
            _records.Remove(obj);

            if (!_scores.ContainsKey(cell.Type))
            {
                _scores.Add(cell.Type, 0);
            }
            _scores[cell.Type] += 1;
        }
        for (int i = 0; i < _textScores.Length; i++)
        {
            if (_scores.TryGetValue(i + 1, out int score))
            {
                _textScores[i].text = score.ToString();
            }
        }
        _board.RefreshCell();
    }

    /// <summary>
    /// 回收所有棋子并重置游戏
    /// </summary>
    /// <param name="needScale">是否需要缩放</param>
    void RecycleAll(bool needScale)
    {
        foreach (var item in _records)
        {
            if (needScale)
            {
                item.Key.localScale = Vector3.zero;
            }
            item.Key.SetParent(transform);
            _factory.Recycle(item.Value, item.Key);
        }
        _records.Clear();
        foreach (var item in _textScores)
        {
            item.text = "0";
        }
        _scores.Clear();
        _board.FillCell();
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

    /// <summary>
    /// 游戏结束重置动画协程
    /// </summary>
    /// <returns></returns>
    IEnumerator Replay()
    {
        _isBusy = true;
        _textOver.text = "GAME OVER";
        yield return new WaitForSeconds(0.5f);
        var time = Time.time + 0.2f;
        while (time > Time.time)
        {
            yield return null;
            var s = Vector3.zero;
            if (time > Time.time)
            {
                s.x = s.y = s.z = (time - Time.time) * 5f;
            }
            foreach (var item in _records)
            {
                item.Key.localScale = s;
            }
        }
        yield return new WaitForSeconds(0.5f);
        RecycleAll(false);
        _textOver.text = "";
        _isBusy = false;
    }

    /// <summary>
    /// 棋子交换动画协程
    /// </summary>
    /// <param name="a">源单元格</param>
    /// <param name="b">目标单元格</param>
    /// <param name="matches">匹配列表</param>
    /// <returns></returns>
    IEnumerator SwapCell(BoardCell a, BoardCell b, List<BoardCell> matches)
    {
        _isBusy = true;
        var from = _map[a.Y, a.X];
        var to = _map[b.Y, b.X];
        var fromPosition = from.position;
        var toPosition = to.position;
        var time = Time.time + 0.2f;
        var t = 0f;
            
        // 交换动画
        while (time > Time.time)
        {
            yield return null;
            t = 1f - (time - Time.time) * 5f;
            from.position = Vector3.Lerp(fromPosition, toPosition, t);
            to.position = Vector3.Lerp(toPosition, fromPosition, t);
        }

        // 如果没有匹配，恢复位置
        if (matches.Count == 0)
        {
            time = Time.time + 0.2f;
            while (time > Time.time)
            {
                yield return null;
                t = 1f - (time - Time.time) * 5f;
                from.position = Vector3.Lerp(toPosition, fromPosition, t);
                to.position = Vector3.Lerp(fromPosition, toPosition, t);
            }
            _isBusy = false;
            yield break;
        }
            
        // 更新映射
        _map[a.Y, a.X] = to;
        _map[b.Y, b.X] = from;
            
        // 消除动画
        time = Time.time + 0.2f;
        while (time > Time.time)
        {
            yield return null;
            var s = Vector3.zero;
            if (time > Time.time)
            {
                s.x = s.y = s.z = (time - Time.time) * 5f;
            }
            foreach (var cell in matches)
            {
                _map[cell.Y, cell.X].localScale = s;
            }
        }
            
        // 回收匹配的棋子
        RecycleMatch(matches);
    }

    /// <summary>
    /// 棋子下落动画协程
    /// </summary>
    /// <param name="offsets">下落偏移量列表</param>
    /// <param name="drops">下落单元格列表</param>
    /// <param name="matches">匹配列表</param>
    /// <returns></returns>
    IEnumerator DropCell(List<int> offsets, List<BoardCell> drops, List<BoardCell> matches)
    {
        _isBusy = true;
        var maxOffset = 0;
        var dict = new Dictionary<Transform, Vector3>();
            
        // 准备下落的棋子
        for (int i = 0; i < offsets.Count; i++)
        {
            var offset = offsets[i];
            var x = drops[i].X;
            var y = drops[i].Y;
            var t = drops[i].Type;
            var position = GetCellPosition(x, y);
            if (maxOffset < offset)
            {
                maxOffset = offset;
            }
                
            // 如果是已有棋子下落
            if (y + offset < _height)
            {
                _map[y, x] = _map[y + offset, x];
            }
            // 如果是新生成的棋子
            else
            {
                var obj = _factory.Get(t);
                obj.position = new Vector3(position.x, position.y + offset, 0f);
                obj.SetParent(transform, true);
                _map[y, x] = obj;
                _records.Add(obj, t);
            }
            dict.Add(_map[y, x], position);
        }
            
        // 下落动画
        maxOffset *= 5;
        for (int i = 0; i < maxOffset; i++)
        {
            yield return null;
            foreach (var item in dict)
            {
                item.Key.position = Vector3.MoveTowards(item.Key.position, item.Value, 0.2f);
            }
        }
            
        // 确保位置准确
        foreach (var item in dict)
        {
            item.Key.position = item.Value;
        }

        // 如果没有新的匹配
        if (matches.Count == 0)
        {
            _board.FindMove();
            _isBusy = false;
            yield break;
        }
            
        // 消除动画
        var time = Time.time + 0.2f;
        while (time > Time.time)
        {
            yield return null;
            var s = Vector3.zero;
            if (time > Time.time)
            {
                s.x = s.y = s.z = (time - Time.time) * 5f;
            }
            foreach (var cell in matches)
            {
                _map[cell.Y, cell.X].localScale = s;
            }
        }
            
        // 回收匹配的棋子
        RecycleMatch(matches);
    }
}
