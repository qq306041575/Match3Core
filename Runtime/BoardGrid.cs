using System;
using System.Collections.Generic;

namespace Match3Core
{
    /// <summary>
    /// 三消游戏核心网格逻辑类
    /// 负责棋盘布局、单元格交换、匹配检测、消除、炸弹、下落、填充
    /// </summary>
    public class BoardGrid
    {
        /// <summary>
        /// 五连消除类型（同色炸弹）
        /// </summary>
        const int MatchType5 = 100;
        
        /// <summary>
        /// 四连垂直消除类型（横线炸弹）
        /// </summary>
        const int MatchType4Y = 101;
        
        /// <summary>
        /// 四连水平消除类型（竖线炸弹）
        /// </summary>
        const int MatchType4X = 102;
        
        /// <summary>
        /// T型消除类型（3x3范围炸弹）
        /// </summary>
        const int MatchTypeT = 103;
        
        /// <summary>
        /// L型消除类型（3x3范围炸弹）
        /// </summary>
        const int MatchTypeL = 104;

        /// <summary>
        /// 棋盘宽度
        /// </summary>
        int _width;
        
        /// <summary>
        /// 棋盘高度
        /// </summary>
        int _height;
        
        /// <summary>
        /// 棋子类型数量
        /// </summary>
        int _count;
        
        /// <summary>
        /// 地图障碍标记，true表示可通行
        /// </summary>
        bool[,] _map;
        
        /// <summary>
        /// 棋盘单元格数组
        /// </summary>
        BoardCell[,] _grid;
        
        /// <summary>
        /// 当前匹配的单元格列表
        /// </summary>
        List<BoardCell> _matches;
        
        /// <summary>
        /// 下落过程中的空位列表
        /// </summary>
        List<BoardCell> _holes;
        
        /// <summary>
        /// 下落偏移量列表
        /// </summary>
        List<int> _offsets;

        /// <summary>
        /// 随机生成类型
        /// </summary>
        Random _random;

        /// <summary>
        /// 移动起始单元格
        /// </summary>
        BoardCell _moveFrom;
        
        /// <summary>
        /// 移动目标单元格
        /// </summary>
        BoardCell _moveTo;

        /// <summary>
        /// 设置爆炸类型事件（x, y, 爆炸类型）
        /// </summary>
        public event Action<int, int, int> OnSetExplosion;
        
        /// <summary>
        /// 设置单元格事件（x, y, 棋子类型）
        /// </summary>
        public event Action<int, int, int> OnSetCell;
        
        /// <summary>
        /// 交换单元格事件（源单元格, 目标单元格, 匹配列表）
        /// </summary>
        public event Action<BoardCell, BoardCell, List<BoardCell>> OnSwapCell;
        
        /// <summary>
        /// 下落单元格事件（偏移量列表, 下落单元格列表, 匹配列表）
        /// </summary>
        public event Action<List<int>, List<BoardCell>, List<BoardCell>> OnDropCell;
        
        /// <summary>
        /// 无解事件（无法继续游戏）
        /// </summary>
        public event Action OnNoMatch;

        /// <summary>
        /// 初始化棋盘网格
        /// </summary>
        /// <param name="width">棋盘宽度</param>
        /// <param name="height">棋盘高度</param>
        /// <param name="count">棋子类型数量</param>
        public BoardGrid(int width, int height, int count)
        {
            _width = width;
            _height = height;
            _count = count;
            _map = new bool[_height, _width];
            _grid = new BoardCell[_height, _width];
            _matches = new List<BoardCell>();
            _holes = new List<BoardCell>();
            _offsets = new List<int>();
            _random = new Random();
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _grid[y, x] = new BoardCell(x, y);
                    _map[y, x] = true;
                }
            }
        }

        /// <summary>
        /// 使用指定类型数组填充棋盘
        /// </summary>
        /// <param name="types">类型数组，按行优先顺序排列</param>
        public void FillCell(int[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                var x = i % _width;
                var y = i / _width;
                if (types[i] > 0)
                {
                    SetCell(x, y, types[i], 0);
                }
                else
                {
                    _map[y, x] = false;
                }
            }
            if (!HasValidMove())
            {
                throw new Exception("error types");
            }
        }

        /// <summary>
        /// 随机填充棋盘（确保初始状态没有匹配）
        /// </summary>
        public void FillCell()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (!_map[y, x])
                    {
                        continue;
                    }
                    var left = 0;
                    var down = 0;
                    var count = _count;
                    // 检查左边是否有两个相同类型的棋子
                    if (x > 1 && _grid[y, x - 1].Type == _grid[y, x - 2].Type && _map[y, x - 1])
                    {
                        left = _grid[y, x - 1].Type;
                        count--;
                    }
                    // 检查下边是否有两个相同类型的棋子
                    if (y > 1 && _grid[y - 1, x].Type == _grid[y - 2, x].Type && _map[y - 1, x])
                    {
                        down = _grid[y - 1, x].Type;
                        count--;
                    }
                    // 随机选择类型，避免产生初始匹配
                    var type = RandomInRange(1, count);
                    if (left > 0 && down > 0)
                    {
                        if (type >= left || type >= down)
                        {
                            type++;
                            if (type >= left && type >= down)
                            {
                                type++;
                            }
                        }
                    }
                    else if (left > 0 && type >= left)
                    {
                        type++;
                    }
                    else if (down > 0 && type >= down)
                    {
                        type++;
                    }
                    SetCell(x, y, type, 0);
                }
            }
            // 如果没有有效移动，重新填充
            if (!HasValidMove())
            {
                FillCell();
            }
        }

        /// <summary>
        /// 尝试交换两个单元格的棋子
        /// </summary>
        /// <param name="fromX">源位置X坐标</param>
        /// <param name="fromY">源位置Y坐标</param>
        /// <param name="toX">目标位置X坐标</param>
        /// <param name="toY">目标位置Y坐标</param>
        /// <returns>是否交换成功</returns>
        public bool SwapCell(int fromX, int fromY, int toX, int toY)
        {
            if (fromX >= 0 && fromX < _width && fromY >= 0 && fromY < _height
                && toX >= 0 && toX < _width && toY >= 0 && toY < _height
                && _map[fromY, fromX] && _map[toY, toX])
            {
                MoveCell(_grid[fromY, fromX], _grid[toY, toX]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 执行自动匹配交换
        /// </summary>
        public void SwapMatch()
        {
            if (_moveFrom != null && _moveTo != null)
            {
                MoveCell(_moveFrom, _moveTo);
            }
        }

        /// <summary>
        /// 检查是否有有效移动，如果没有则触发无解事件
        /// </summary>
        public void FindMove()
        {
            if (!HasValidMove() && OnNoMatch != null)
            {
                OnNoMatch();
            }
        }

        /// <summary>
        /// 刷新单元格（执行消除和下落）
        /// </summary>
        public void RefreshCell()
        {
            AddExplosion();
            RemoveMatch();
        }

        /// <summary>
        /// 设置单元格的类型和爆炸类型
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="type">棋子类型</param>
        /// <param name="explodeType">爆炸类型</param>
        void SetCell(int x, int y, int type, int explodeType)
        {
            _grid[y, x].Type = type;
            _grid[y, x].ExplodeType = explodeType;
            if (OnSetCell != null)
            {
                OnSetCell(x, y, type);
            }
            if (explodeType > MatchType5 && OnSetExplosion != null)
            {
                OnSetExplosion(x, y, explodeType);
            }
        }

        /// <summary>
        /// 移动两个单元格的棋子
        /// </summary>
        /// <param name="from">源单元格</param>
        /// <param name="to">目标单元格</param>
        void MoveCell(BoardCell from, BoardCell to)
        {
            var a = from.Type;
            var b = to.Type;
            var c = from.ExplodeType;
            var d = to.ExplodeType;
            from.Type = b;
            to.Type = a;
            from.ExplodeType = d;
            to.ExplodeType = c;
            
            // 检查是否有匹配
            FindMatch();
            FindExplosion(from, to);
            
            // 如果没有匹配，恢复交换
            if (_matches.Count == 0)
            {
                from.Type = a;
                to.Type = b;
                from.ExplodeType = c;
                to.ExplodeType = d;
            }
            else
            {
                _moveFrom = from;
                _moveTo = to;
            }
            
            // 触发交换事件
            if (OnSwapCell != null)
            {
                OnSwapCell(from, to, _matches);
            }
        }

        /// <summary>
        /// 检查是否存在有效的移动
        /// </summary>
        /// <returns>是否存在有效移动</returns>
        bool HasValidMove()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (!_map[y, x])
                    {
                        continue;
                    }
                    _moveFrom = _grid[y, x];
                    var t = _moveFrom.Type;
                    
                    // 同色炸弹可以和任何相邻棋子交换
                    if (t == MatchType5)
                    {
                        if (x + 1 < _width && _map[y, x + 1])
                        {
                            _moveTo = _grid[y, x + 1];
                            return true;
                        }
                        if (x - 1 >= 0 && _map[y, x - 1])
                        {
                            _moveTo = _grid[y, x - 1];
                            return true;
                        }
                        if (y + 1 < _height && _map[y + 1, x])
                        {
                            _moveTo = _grid[y + 1, x];
                            return true;
                        }
                        if (y - 1 >= 0 && _map[y - 1, x])
                        {
                            _moveTo = _grid[y - 1, x];
                            return true;
                        }
                    }
                    
                    // 检查向左移动是否能形成匹配
                    if ((x >= 1 && y >= 1 && y + 1 < _height && t == _grid[y + 1, x - 1].Type && t == _grid[y - 1, x - 1].Type)
                        || (x >= 1 && y + 2 < _height        && t == _grid[y + 1, x - 1].Type && t == _grid[y + 2, x - 1].Type)
                        || (x >= 1 && y - 2 >= 0             && t == _grid[y - 1, x - 1].Type && t == _grid[y - 2, x - 1].Type)
                        || (x >= 3                           && t == _grid[y, x - 2].Type     && t == _grid[y, x - 3].Type))
                    {
                        if (_map[y, x - 1])
                        {
                            _moveTo = _grid[y, x - 1];
                            return true;
                        }
                    }
                    
                    // 检查向右移动是否能形成匹配
                    if ((x + 1 < _width && y >= 1 && y + 1 < _height && t == _grid[y + 1, x + 1].Type && t == _grid[y - 1, x + 1].Type)
                        || (x + 1 < _width && y + 2 < _height        && t == _grid[y + 1, x + 1].Type && t == _grid[y + 2, x + 1].Type)
                        || (x + 1 < _width && y - 2 >= 0             && t == _grid[y - 1, x + 1].Type && t == _grid[y - 2, x + 1].Type)
                        || (x + 3 < _width                           && t == _grid[y, x + 2].Type     && t == _grid[y, x + 3].Type))
                    {
                        if (_map[y, x + 1])
                        {
                            _moveTo = _grid[y, x + 1];
                            return true;
                        }
                    }
                    
                    // 检查向上移动是否能形成匹配
                    if ((y >= 1 && x >= 1 && x + 1 < _width && t == _grid[y - 1, x + 1].Type && t == _grid[y - 1, x - 1].Type)
                        || (y >= 1 && x + 2 < _width        && t == _grid[y - 1, x + 1].Type && t == _grid[y - 1, x + 2].Type)
                        || (y >= 1 && x - 2 >= 0            && t == _grid[y - 1, x - 1].Type && t == _grid[y - 1, x - 2].Type)
                        || (y >= 3                          && t == _grid[y - 2, x].Type     && t == _grid[y - 3, x].Type))
                    {
                        if (_map[y - 1, x])
                        {
                            _moveTo = _grid[y - 1, x];
                            return true;
                        }
                    }
                    
                    // 检查向下移动是否能形成匹配
                    if ((y + 1 < _height && x >= 1 && x + 1 < _width && t == _grid[y + 1, x + 1].Type && t == _grid[y + 1, x - 1].Type)
                        || (y + 1 < _height && x + 2 < _width        && t == _grid[y + 1, x + 1].Type && t == _grid[y + 1, x + 2].Type)
                        || (y + 1 < _height && x - 2 >= 0            && t == _grid[y + 1, x - 1].Type && t == _grid[y + 1, x - 2].Type)
                        || (y + 3 < _height                          && t == _grid[y + 2, x].Type     && t == _grid[y + 3, x].Type))
                    {
                        if (_map[y + 1, x])
                        {
                            _moveTo = _grid[y + 1, x];
                            return true;
                        }
                    }
                }
            }
            _moveFrom = null;
            return false;
        }

        /// <summary>
        /// 查找所有匹配的单元格
        /// </summary>
        void FindMatch()
        {
            _matches.Clear();
            int n;
            
            // 检查水平方向匹配
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width - 2; x++)
                {
                    n = x + 1;
                    while (n < _width && _grid[y, x].Type == _grid[y, n].Type)
                    {
                        n++;
                    }
                    if (n - x < 3)
                    {
                        continue;
                    }
                    while (x < n)
                    {
                        AddMatch(_grid[y, x]);
                        x++;
                    }
                    x--;
                }
            }
            
            // 检查垂直方向匹配
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height - 2; y++)
                {
                    n = y + 1;
                    while (n < _height && _grid[y, x].Type == _grid[n, x].Type)
                    {
                        n++;
                    }
                    if (n - y < 3)
                    {
                        continue;
                    }
                    while (y < n)
                    {
                        AddMatch(_grid[y, x]);
                        y++;
                    }
                    y--;
                }
            }
        }

        /// <summary>
        /// 查找炸弹匹配的单元格
        /// </summary>
        /// <param name="from">源单元格</param>
        /// <param name="to">目标单元格</param>
        void FindExplosion(BoardCell from, BoardCell to)
        {
            if (from.Type == MatchType5 && to.Type == MatchType5)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        AddMatch(_grid[y, x]);
                    }
                }
            }
            else if (from.Type == MatchType5)
            {
                from.ExplodeType = to.Type;
                AddMatch(from);
            }
            else if (to.Type == MatchType5)
            {
                to.ExplodeType = from.Type;
                AddMatch(to);
            }
        }

        /// <summary>
        /// 检测五连、四连、T型、L型，并添加炸弹
        /// </summary>
        void AddExplosion()
        {
            int x, y, t, n;
            
            // 检测四连和五连，并添加炸弹
            for (int i = 0; i < _matches.Count; i++)
            {
                x = _matches[i].X;
                y = _matches[i].Y;
                t = _matches[i].Type;
                n = 0;
                if (t == 0)
                {
                    continue;
                }
                for (int j = x + 1; j < _width; j++)
                {
                    if (t != _grid[y, j].Type)
                    {
                        break;
                    }
                    n++;
                }
                if (n >= 3)
                {
                    for (int k = x; k <= x + n; k++)
                    {
                        _grid[y, k].Type = 0;
                    }
                    if ((_moveFrom.X == x + 1 && _moveFrom.Y == y) || (_moveTo.X == x + 1 && _moveTo.Y == y))
                    {
                        ReplaceCell(x + 1, y, t, n == 3 ? MatchType4X : MatchType5);
                    }
                    else if ((_moveFrom.X == x + 2 && _moveFrom.Y == y) || (_moveTo.X == x + 2 && _moveTo.Y == y))
                    {
                        ReplaceCell(x + 2, y, t, n == 3 ? MatchType4X : MatchType5);
                    }
                    else
                    {
                        ReplaceCell(x, y, t, n == 3 ? MatchType4X : MatchType5);
                    }
                    i--;
                    continue;
                }
                n = 0;
                for (int j = y + 1; j < _height; j++)
                {
                    if (t != _grid[j, x].Type)
                    {
                        break;
                    }
                    n++;
                }
                if (n >= 3)
                {
                    for (int k = y; k <= y + n; k++)
                    {
                        _grid[k, x].Type = 0;
                    }
                    if ((_moveFrom.X == x && _moveFrom.Y == y + 1) || (_moveTo.X == x && _moveTo.Y == y + 1))
                    {
                        ReplaceCell(x, y + 1, t, n == 3 ? MatchType4Y : MatchType5);
                    }
                    else if ((_moveFrom.X == x && _moveFrom.Y == y + 2) || (_moveTo.X == x && _moveTo.Y == y + 2))
                    {
                        ReplaceCell(x, y + 2, t, n == 3 ? MatchType4Y : MatchType5);
                    }
                    else
                    {
                        ReplaceCell(x, y, t, n == 3 ? MatchType4Y : MatchType5);
                    }
                    i--;
                    continue;
                }
            }

            // 检测T型和L型匹配，并添加炸弹
            for (int i = 0; i < _matches.Count; i++)
            {
                x = _matches[i].X;
                y = _matches[i].Y;
                t = _matches[i].Type;
                if (t == 0)
                {
                    continue;
                }
                else if (x + 2 < _width && y + 2 < _height
                    && t == _grid[y, x + 1].Type
                    && t == _grid[y, x + 2].Type
                    && t == _grid[y + 1, x + 1].Type
                    && t == _grid[y + 2, x + 1].Type)
                {
                    _grid[y, x + 1].Type = _grid[y, x + 2].Type = _grid[y + 1, x + 1].Type = _grid[y + 2, x + 1].Type = 0;
                    ReplaceCell(x + 1, y, t, MatchTypeT);
                    i--;
                }
                else if (x + 2 < _width && y + 2 < _height
                    && t == _grid[y, x + 1].Type
                    && t == _grid[y, x + 2].Type
                    && t == _grid[y + 1, x + 2].Type
                    && t == _grid[y + 2, x + 2].Type)
                {
                    _grid[y, x + 1].Type = _grid[y, x + 2].Type = _grid[y + 1, x + 2].Type = _grid[y + 2, x + 2].Type = 0;
                    ReplaceCell(x + 2, y, t, MatchTypeL);
                    i--;
                }
                else if (x + 2 < _width && y + 2 < _height
                    && t == _grid[y + 1, x].Type
                    && t == _grid[y + 2, x].Type
                    && t == _grid[y + 1, x + 1].Type
                    && t == _grid[y + 1, x + 2].Type)
                {
                    _grid[y + 1, x].Type = _grid[y + 2, x].Type = _grid[y + 1, x + 1].Type = _grid[y + 1, x + 2].Type = 0;
                    ReplaceCell(x, y + 1, t, MatchTypeT);
                    i--;
                }
                else if (x + 2 < _width && y + 2 < _height
                    && t == _grid[y + 1, x].Type
                    && t == _grid[y + 2, x].Type
                    && t == _grid[y + 2, x + 1].Type
                    && t == _grid[y + 2, x + 2].Type)
                {
                    _grid[y + 1, x].Type = _grid[y + 2, x].Type = _grid[y + 2, x + 1].Type = _grid[y + 2, x + 2].Type = 0;
                    ReplaceCell(x, y + 2, t, MatchTypeL);
                    i--;
                }
                else if (x + 2 < _width && y - 2 >= 0
                    && t == _grid[y - 1, x].Type
                    && t == _grid[y - 2, x].Type
                    && t == _grid[y - 1, x + 1].Type
                    && t == _grid[y - 1, x + 2].Type)
                {
                    _grid[y - 1, x].Type = _grid[y - 2, x].Type = _grid[y - 1, x + 1].Type = _grid[y - 1, x + 2].Type = 0;
                    ReplaceCell(x, y - 1, t, MatchTypeT);
                    i--;
                }
                else if (x + 2 < _width && y - 2 >= 0
                    && t == _grid[y - 1, x].Type
                    && t == _grid[y - 2, x].Type
                    && t == _grid[y - 2, x + 1].Type
                    && t == _grid[y - 2, x + 2].Type)
                {
                    _grid[y - 1, x].Type = _grid[y - 2, x].Type = _grid[y - 2, x + 1].Type = _grid[y - 2, x + 2].Type = 0;
                    ReplaceCell(x, y - 2, t, MatchTypeL);
                    i--;
                }
                else if (x + 2 < _width && y - 2 >= 0
                    && t == _grid[y, x + 1].Type
                    && t == _grid[y, x + 2].Type
                    && t == _grid[y - 1, x + 1].Type
                    && t == _grid[y - 2, x + 1].Type)
                {
                    _grid[y, x + 1].Type = _grid[y, x + 2].Type = _grid[y - 1, x + 1].Type = _grid[y - 2, x + 1].Type = 0;
                    ReplaceCell(x + 1, y, t, MatchTypeT);
                    i--;
                }
                else if (x + 2 < _width && y - 2 >= 0
                    && t == _grid[y, x + 1].Type
                    && t == _grid[y, x + 2].Type
                    && t == _grid[y - 1, x + 2].Type
                    && t == _grid[y - 2, x + 2].Type)
                {
                    _grid[y, x + 1].Type = _grid[y, x + 2].Type = _grid[y - 1, x + 2].Type = _grid[y - 2, x + 2].Type = 0;
                    ReplaceCell(x + 2, y, t, MatchTypeL);
                    i--;
                }
                else if (x + 2 < _width && y + 1 < _height && y - 1 >= 0
                    && t == _grid[y, x + 1].Type
                    && t == _grid[y, x + 2].Type
                    && t == _grid[y + 1, x].Type
                    && t == _grid[y - 1, x].Type)
                {
                    _grid[y, x + 1].Type = _grid[y, x + 2].Type = _grid[y + 1, x].Type = _grid[y - 1, x].Type = 0;
                    ReplaceCell(x, y, t, MatchTypeT);
                    i--;
                }
                else if (x + 2 < _width && y + 1 < _height && y - 1 >= 0
                    && t == _grid[y, x + 1].Type
                    && t == _grid[y, x + 2].Type
                    && t == _grid[y + 1, x + 1].Type
                    && t == _grid[y - 1, x + 1].Type)
                {
                    _grid[y, x + 1].Type = _grid[y, x + 2].Type = _grid[y + 1, x + 1].Type = _grid[y - 1, x + 1].Type = 0;
                    ReplaceCell(x + 1, y, t, MatchTypeT);
                    i--;
                }
                else if (x + 2 < _width && y + 1 < _height && y - 1 >= 0
                    && t == _grid[y, x + 1].Type
                    && t == _grid[y, x + 2].Type
                    && t == _grid[y + 1, x + 2].Type
                    && t == _grid[y - 1, x + 2].Type)
                {
                    _grid[y, x + 1].Type = _grid[y, x + 2].Type = _grid[y + 1, x + 2].Type = _grid[y - 1, x + 2].Type = 0;
                    ReplaceCell(x + 2, y, t, MatchTypeT);
                    i--;
                }
            }
        }

        /// <summary>
        /// 替换单元格为炸弹
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="type">原始类型</param>
        /// <param name="explodeType">爆炸类型</param>
        void ReplaceCell(int x, int y, int type, int explodeType)
        {
            _matches.Remove(_grid[y, x]);
            if (explodeType == MatchType5)
            {
                SetCell(x, y, explodeType, type);
            }
            else
            {
                SetCell(x, y, type, explodeType);
            }
        }

        /// <summary>
        /// 添加匹配的单元格，并处理炸弹效果
        /// </summary>
        /// <param name="cell">要添加的单元格</param>
        void AddMatch(BoardCell cell)
        {
            if (_matches.Contains(cell) || !_map[cell.Y, cell.X])
            {
                return;
            }
            _matches.Add(cell);

            // 同色炸弹：消除所有相同类型
            if (cell.Type == MatchType5)
            {
                var type = cell.ExplodeType;
                cell.ExplodeType = 0;
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        if (type == _grid[y, x].Type)
                        {
                            AddMatch(_grid[y, x]);
                        }
                    }
                }
            }
            switch (cell.ExplodeType)
            {
                case MatchType4Y: // 横线炸弹：消除整行
                    cell.ExplodeType = 0;
                    for (int x = 0; x < _width; x++)
                    {
                        AddMatch(_grid[cell.Y, x]);
                    }
                    break;
                case MatchType4X: // 竖线炸弹：消除整列
                    cell.ExplodeType = 0;
                    for (int y = 0; y < _height; y++)
                    {
                        AddMatch(_grid[y, cell.X]);
                    }
                    break;
                case MatchTypeT: // T型炸弹：消除3x3范围
                case MatchTypeL: // L型炸弹：消除3x3范围
                    cell.ExplodeType = 0;
                    for (int y = cell.Y - 1; y <= cell.Y + 1; y++)
                    {
                        for (int x = cell.X - 1; x <= cell.X + 1; x++)
                        {
                            if (x >= 0 && x < _width && y >= 0 && y < _height)
                            {
                                AddMatch(_grid[y, x]);
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 移除匹配的单元格并执行下落填充
        /// </summary>
        void RemoveMatch()
        {
            _offsets.Clear();
            _holes.Clear();
            var index = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (!_map[y, x])
                    {
                        continue;
                    }
                    // 标记匹配的单元格为空位
                    if (_matches.Contains(_grid[y, x]))
                    {
                        _holes.Add(_grid[y, x]);
                    }
                    // 将上方的棋子下落到空位
                    else if (index < _holes.Count)
                    {
                        _offsets.Add(_grid[y, x].Y - _holes[index].Y);
                        _holes[index].Type = _grid[y, x].Type;
                        _holes[index].ExplodeType = _grid[y, x].ExplodeType;
                        _holes.Add(_grid[y, x]);
                        index++;
                    }
                }
                // 填充新棋子到顶部空位
                for (int i = index, j = 0; i < _holes.Count; i++, j++)
                {
                    _offsets.Add(_height - _holes[i].Y + j);
                    _holes[i].Type = RandomInRange(1, _count);
                    _holes[i].ExplodeType = 0;
                    index++;
                }
            }
            
            // 检查新的匹配
            FindMatch();
            
            // 触发下落事件
            if (OnDropCell != null)
            {
                OnDropCell(_offsets, _holes, _matches);
            }
        }

        /// <summary>
        /// 在指定范围内获取随机整数（包含边界）
        /// </summary>
        /// <param name="minInclusive">最小值（包含）</param>
        /// <param name="maxInclusive">最大值（包含）</param>
        /// <returns>随机整数</returns>
        int RandomInRange(int minInclusive, int maxInclusive)
        {
            return _random.Next(minInclusive, maxInclusive + 1);
        }
    }
}
