namespace Match3Core
{
    /// <summary>
    /// 棋盘单元格类
    /// 表示棋盘中的一个格子，包含位置、棋子类型和爆炸类型信息
    /// </summary>
    public class BoardCell
    {
        /// <summary>
        /// 单元格的X坐标
        /// </summary>
        public int X;
        
        /// <summary>
        /// 单元格的Y坐标
        /// </summary>
        public int Y;
        
        /// <summary>
        /// 棋子类型（1-n表示普通棋子，100+表示特殊道具）
        /// </summary>
        public int Type;
        
        /// <summary>
        /// 爆炸类型（0表示无，101-104表示不同类型的炸弹）
        /// </summary>
        public int ExplodeType;

        /// <summary>
        /// 初始化单元格
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        public BoardCell(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}