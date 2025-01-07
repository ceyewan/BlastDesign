using MathNet.Spatial.Euclidean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlastDesign.tool.BlackBoxTest
{

  /// <summary>
  /// 炮孔坐标
  /// </summary>
  public class HolePosition
  {
    public HolePosition()
    {
      Top = new Point3D();
      Bottom = new Point3D();
    }

    /// <summary>
    /// 炮孔顶部坐标
    /// </summary>
    public Point3D Top
    {
      set; get;
    }

    /// <summary>
    /// 炮孔底部坐标
    /// </summary>
    public Point3D Bottom
    {
      set; get;
    }

    /// <summary>
    /// 炮孔直径
    /// </summary>
    public double Diameter
    {
      set; get;
    }

    /// <summary>
    /// 炮孔中点坐标
    /// </summary>
    public Point3D Middle
    {
      get
      {
        //return (Top + Bottom) / 2;
        return Point3D.MidPoint(Top, Bottom);
      }
    }



    /// <summary>
    /// 绘图直径
    /// </summary>
    public double DrawD
    {
      set; get;
    }

    public enum HoleType
    {
      MainBlastHole,
      PreSplitHole,
      BufferHole
    }
    /// <summary>
    /// 炮孔类型
    /// </summary>
    public HoleType HoleStyle
    {
      set; get;
    }

    /// <summary>
    /// 炮孔行号
    /// </summary>
    public int RowId
    {
      set; get;
    }

    /// <summary>
    /// 炮孔列号
    /// </summary>
    public int ColumnId
    {
      set; get;
    }

    /// <summary>
    /// 炮孔编号
    /// </summary>
    public int HoleId
    {
      set; get;
    }

    /// <summary>
    /// 所在导爆索编号
    /// </summary>
    public int BlastId
    {
      set; get;
    }

    /// <summary>
    /// 炮孔装药量
    /// </summary>
    public double Q
    {
      set; get;
    }

    /// <summary>
    /// 炮孔堵塞长度
    /// </summary>
    public double LD
    {
      set; get;
    }

    /// <summary>
    /// 炮孔长度
    /// </summary>
    public double L
    {
      get
      {
        return Top.DistanceTo(Bottom);
      }
    }

    /// <summary>
    /// 炮孔装药长度
    /// </summary>
    public double LY
    {
      get
      {
        return L - LA - LD;
      }

    }

    /// <summary>
    /// 空气段长度
    /// </summary>
    public double LA
    {
      set; get;
    }

    /// <summary>
    /// 药卷数目
    /// </summary>
    public int LN
    {
      set; get;
    }

    /// <summary>
    /// 炮孔是否合法
    /// </summary>
    public bool IsValid
    {
      set; get;
    }

    /// <summary>
    /// 炮孔是否移动
    /// </summary>
    public bool IsMoved
    {
      set; get;
    }

    /// <summary>
    /// 该炮孔所在的轮廓绘图起始坐标
    /// </summary>
    public Point3D StartPoint
    {
      set; get;
    }

    /// <summary>
    /// 炮孔圆心坐标
    /// </summary>
    public Point3D CenterPoint
    {
      set; get;
    }

    /// <summary>
    /// 炮孔所在轮廓面方向
    /// </summary>
    public Vector3D OutLineVector
    {
      set; get;
    }

    /// <summary>
    /// 炮孔所在边界内法线方向
    /// </summary>
    public Vector3D OutLineInnerNorm
    {
      set; get;
    }

    /// <summary>
    /// 该炮孔所在的轮廓绘图起始坐标
    /// </summary>
    public Point3D BottomStartPoint
    {
      set; get;
    }

    /// <summary>
    /// 炮孔圆心坐标
    /// </summary>
    public Point3D BottomCenterPoint
    {
      set; get;
    }

    /// <summary>
    /// 炮孔所在轮廓面方向
    /// </summary>
    public Vector3D BottomOutLineVector
    {
      set; get;
    }

    /// <summary>
    /// 炮孔所在边界内法线方向
    /// </summary>
    public Vector3D BottomOutLineInnerNorm
    {
      set; get;
    }

    public Vector3D HoleVector
    {
      get
      {
        return this.Top - this.Bottom;
      }
    }


  }
}
