//the basic data srtucture representing the position in dog egg editor
using UnityEngine;
[System.Serializable]
public struct DEPosition
{
	private int _x;
	private int _y;
	private int _z;

	private static Vector3 p0 = new Vector3(-0.5f, 0.5f, 0.5f);
	private static Vector3 p1 = new Vector3(-0.5f, -0.5f, 0.5f);
	private static Vector3 p2 = new Vector3(0.5f, -0.5f, 0.5f);
	private static Vector3 p3 = new Vector3(0.5f, 0.5f, 0.5f);
	private static Vector3 p4 = new Vector3(0.5f, 0.5f, -0.5f);
	private static Vector3 p5 = new Vector3(0.5f, -0.5f, -0.5f);
	private static Vector3 p6 = new Vector3(-0.5f, -0.5f, -0.5f);
	private static Vector3 p7 = new Vector3(-0.5f, 0.5f, -0.5f);

	private static Vector3[] upVertexs = { p0, p1, p2, p3 };
	private static Vector3[] forwardVertexs = { p0, p3, p4, p7 };
	private static Vector3[] rightVertexs = { p5, p4, p3, p2 };
	private static Vector3[] downVertexs = { p4, p5, p6, p7 };
	private static Vector3[] backVertexs = { p6, p5, p2, p1 };
	private static Vector3[] leftVertexs = { p1, p0, p7, p6 };

	public DEPosition(int x, int y, int z)
	{
		_x = x;
		_y = y;
		_z = z;
	}

	public int x {
		set {
			_x = value;
		}
		get {
			return _x;
		}
	}

	public int y {
		set {
			_y = value;
		}
		get {
			return _y;
		}
	}

	public int z {
		set {
			_z = value;
		}
		get {
			return _z;
		}
	}

	public void set(int x, int y, int z)
	{
		_x = x;
		_y = y;
		_z = z;
	}

	/// <summary>
	/// Returns (x+1, y, z)
	/// </summary>
	public DEPosition right {
		get {
			return new DEPosition(_x + 1, _y, _z);
		}
	}

	/// <summary>
	/// Returns(x-1, y, z)
	/// </summary>
	public DEPosition left {
		get {
			return new DEPosition(_x - 1, _y, _z);
		}
	}

	/// <summary>
	/// Returns(x, y, z+1)
	/// </summary>
	public DEPosition up {
		get {
			return new DEPosition(_x, _y, _z + 1);
		}
	}

	/// <summary>
	/// Returns(x, y, z-1)
	/// </summary>
	public DEPosition down {
		get {
			return new DEPosition(_x, _y, _z - 1);
		}
	}

	/// <summary>
	/// Returns(x, y+1, z)
	/// </summary>
	public DEPosition forward {
		get {
			return new DEPosition(_x, _y + 1, _z);
		}
	}

	/// <summary>
	/// Returns(x, y-1, z)
	/// </summary>
	public DEPosition back {
		get {
			return new DEPosition(_x, _y - 1, _z);
		}
	}

	/// <summary>
	/// Returns the basic (1,1,1)
	/// </summary>
	public static DEPosition one {
		get {
			return new DEPosition(1, 1, 1);
		}
	}

	/// <summary>
	/// Returns the basic (0,0,0)
	/// </summary>
	public static DEPosition zero {
		get {
			return new DEPosition(0, 0, 0);
		}
	}

	public DEPosition GetDirection(DEDirection dir)
	{
		switch (dir) {
			case DEDirection.right:
				return right;
			case DEDirection.left:
				return left;
			case DEDirection.forward:
				return forward;
			case DEDirection.back:
				return back;
			case DEDirection.up:
				return up;
			case DEDirection.down:
				return down;
			default:
				return this;
		}
	}

	public DEPosition GetOppositeDirection(DEDirection dir)
	{
		switch (dir) {
			case DEDirection.right:
				return left;
			case DEDirection.left:
				return right;
			case DEDirection.forward:
				return back;
			case DEDirection.back:
				return forward;
			case DEDirection.up:
				return down;
			case DEDirection.down:
				return up;
			default:
				return this;
		}
	}

	/// <summary>
	/// Gets the 4 vertexs of a side locally
	/// </summary>
	/// <returns>The quad local vertexs.</returns>
	/// <param name="unitPosition">Unit position.</param>
	/// <param name="quadSide">Quad side.</param>
	public static Vector3[] GetQuadLocalVertexs(DEPosition unitPosition, DEDirection quadSide)
	{
		Vector3[] verts = new Vector3[4];
		Vector3[] result = new Vector3[4];
		switch (quadSide) {
			case DEDirection.right:
				verts = rightVertexs;
				break;
			case DEDirection.left:
				verts = leftVertexs;
				break;
			case DEDirection.forward:
				verts = forwardVertexs;
				break;
			case DEDirection.back:
				verts = backVertexs;
				break;
			case DEDirection.up:
				verts = upVertexs;
				break;
			case DEDirection.down:
				verts = downVertexs;
				break;
		}
		for (int i = 0; i < 4; i++) {
			result[i] = verts[i] + (Vector3)unitPosition;
		}
		return result;
	}

	/// <summary>
	/// Get the 4 vertex of a side and transfer it to global points 
	/// </summary>
	/// <returns>The quad globle vertexs.</returns>
	/// <param name="unitPosition">Unit position.</param>
	/// <param name="quadSide">Quad side.</param>
	/// <param name="scene">Scene.</param>
	public static Vector3[] GetQuadWorldVertexs(DEPosition unitPosition, DEDirection quadSide, GameObject scene)
	{
		Vector3[] res = GetQuadLocalVertexs(unitPosition, quadSide);
		Matrix4x4 matrix = scene.transform.localToWorldMatrix;
		for (int i = 0; i < 4; i++) {
			//transfer from local to global
			res[i] = matrix.MultiplyPoint3x4(res[i]);
		}
		return res;
	}

	/// <summary>
	/// Giving the already got matrtix makes the program slightly faster
	/// </summary>
	/// <returns>The quad globle vertexs.</returns>
	public static Vector3[] GetQuadWorldVertexs(DEPosition unitPosition, DEDirection quadSide, Matrix4x4 localToWorld)
	{
		Vector3[] local = GetQuadLocalVertexs(unitPosition, quadSide);
		Vector3[] world = new Vector3[4];
		for (int i = 0; i < 4; i++) {
			//transfer from local to global
			world[i] = localToWorld.MultiplyPoint3x4(local[i]);
		}
		return world;
	}

	/// <summary>
	/// Transfer a local position in de editor to a world position
	/// </summary>
	/// <param name="localPosition">Local position.</param>
	/// <param name="scene">the de scene.</param>
	public static Vector3 LocalToWorld(Vector3 localPosition, GameObject scene)
	{
		return (scene.transform.localToWorldMatrix.MultiplyPoint3x4((Vector3)localPosition));
	}

	/// <summary>
	/// transfer a world position to a local position relative the the de scene
	/// </summary>
	public static Vector3 WorldToLocal(Vector3 worldPosition, GameObject scene)
	{
		return (scene.transform.worldToLocalMatrix.MultiplyPoint3x4(worldPosition));
	}

	/// <summary>
	/// Transfer from a string to a deposition
	/// </summary>
	/// <param name="position">Position.</param>
	public static DEPosition NameToPosition(string name)
	{
		string[] allNums = name.Split(',');
		int[] nums = { -1, -1, -1 };

		for (int i = 0; i < allNums.Length; i++) {
			if (!int.TryParse(allNums[i], out nums[i])) {
				Debug.LogError("some thing went wrong with the input name");
			}
		}

		return new DEPosition(nums[0], nums[1], nums[2]);
	}

	/// <summary>
	/// Return the string of deposition
	/// </summary>
	/// <returns>The to name.</returns>
	/// <param name="position">Position.</param>
	public static string PositionToName(DEPosition position)
	{
		return position.ToString();
	}

	/// <summary>
	/// returns a string "xyz" consists of x, y, z values.
	/// </summary>
	public override string ToString()
	{
		return (_x + "," + _y + "," + _z);
	}

	public override int GetHashCode()
	{
		return _x.GetHashCode() ^ _y.GetHashCode() ^ _z.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is DEPosition) {
			return (_x == ((DEPosition)obj).x && _y == ((DEPosition)obj).y && _z == ((DEPosition)obj).z);
		} else {
			return false;
		}
	}

	public static DEPosition operator +(DEPosition a, DEPosition b)
	{
		return new DEPosition(a.x + b.x, a.y + b.y, a.z + b.z);
	}

	//probably never needed, just in case
	public static DEPosition operator /(DEPosition a, int b)
	{
		return new DEPosition(a.x / b, a.y / b, a.z / b);
	}

	public static bool operator ==(DEPosition lhs, DEPosition rhs)
	{
		return (lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z);
	}

	public static bool operator !=(DEPosition lhs, DEPosition rhs)
	{
		return !(lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z);
	}

	public static DEPosition operator *(DEPosition a, int b)
	{
		return new DEPosition(a.x * b, a.y * b, a.z * b);
	}

	public static DEPosition operator *(int b, DEPosition a)
	{
		return new DEPosition(a.x * b, a.y * b, a.z * b);
	}

	public static DEPosition operator -(DEPosition a, DEPosition b)
	{
		return new DEPosition(a.x - b.x, a.y - b.y, a.z - b.z);
	}

	public static DEPosition operator -(DEPosition a)
	{
		return new DEPosition(-a.x, -a.y, -a.z);
	}

	public static explicit operator Vector3(DEPosition a)
	{
		return new Vector3(a.x, a.y, a.z);
	}

	public static explicit operator DEPosition(Vector3 a)
	{
		return new DEPosition(Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), Mathf.RoundToInt(a.z));
	}
}
