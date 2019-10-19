using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseChunk
{
	//WARNING: this class doesn't check if the input position is within the chunk,
	//make sure the position is within this chunk before pass the position into this class!
	//this is the data of a chunk of cubes
	private bool[] units; //true means there is a cube, false other wise
	private Vector3 chunkPos; //what is the position of the whole chunk(start from (0,0), add one for each chunk)

	public BaseChunk(Vector3 chunkPos)
	{
		this.units = new bool[2048];
		this.chunkPos = new Vector3();

		for (int i = 0; i < 2048; i++) {
			units[i] = false;
		}
		this.chunkPos = chunkPos;
	}

	public void AddUnit(DEPosition pos)
	{
		//Debug.Log("positing: " + pos + " index:" + GetIndex(pos));
		units[GetIndex(pos)] = true;
	}

	public void RemoveUnit(DEPosition pos)
	{
		units[GetIndex(pos)] = false;
	}

	public bool HasUnit(DEPosition pos)
	{
		return units[GetIndex(pos)];
	}

	//return the list of cubes that has less than 6 cubes around them
	public List<DEPosition> GetVisibleUnits()
	{
		List<DEPosition> result = new List<DEPosition>();
		for (int i = 0; i < 2048; i++) {
			if (!units[i]) continue;
			if (IsVisible(i)) {
				result.Add(GetPos(i));
				continue;
			}
		}
		return result;
	}

	/// <summary>
	/// Get the visible units relative to the chunk it self
	/// </summary>
	/// <returns>The local visible units.</returns>
	public List<DEPosition> GetLocalVisibleUnits()
	{
		List<DEPosition> result = new List<DEPosition>();
		for (int i = 0; i < 2048; i++) {
			if (!units[i]) continue;
			if (IsVisible(i)) {
				result.Add(GetLocalPos(i));
				//Debug.Log("local visible: " + GetLocalPos(i) + " index: " + i);
				continue;
			}
		}

		return result;
	}

	public List<DEPosition> GetAllUnits()
	{
		List<DEPosition> allUnits = new List<DEPosition>();
		for (int i = 0; i < 2048; i++) {
			if (units[i]) allUnits.Add(GetPos(i));
		}
		return allUnits;
	}

	public bool IsVisible(DEPosition position)
	{
		int index = GetIndex(position);
		return (IsVisible(index));
	}

	private bool IsVisible(int index)
	{
		int right = index + 1;
		if (right % 16 < index % 16) return true;
		int left = index - 1;
		if (left % 16 > index % 16) return true;
		int forward = index + 16;
		if (forward % 256 < index % 256) return true;
		int back = index - 16;
		if (back % 256 > index % 256) return true;
		int up = index + 256;
		if (up > 2047) return true;
		int down = index - 256;
		if (down < 0) return true;

		return !(units[right] && units[left] && units[up] && units[down] && units[forward] && units[back]);
	}

	private int GetIndex(DEPosition pos)
	{
		//to be tested
		int index = (pos.x - (int)chunkPos.x * 16) + ((pos.y - (int)chunkPos.y * 16) * 16) + ((pos.z - (int)chunkPos.z * 8) * 256);
		//Debug.Log(pos + "index: " + index + "chunk pos:" + chunkPos);
		return index;
	}

	/// <summary>
	/// Get the position relative to this chunk
	/// </summary>
	/// <returns>The local position.</returns>
	/// <param name="index">Index.</param>
	private DEPosition GetLocalPos(int index)
	{
		//Debug.Log("index: " + index + "localPos: " + new DEPosition(index % 16, (index % 256) / 16, index / 256));
		return new DEPosition(index % 16, (index % 256) / 16, index / 256);
	}

	private DEPosition GetPos(int index)
	{
		//to be tested
		return new DEPosition(index % 16 + (int)chunkPos.x * 16, (index % 256) / 16 + (int)chunkPos.y * 16, index / 256 + (int)chunkPos.z * 8);
	}
}
