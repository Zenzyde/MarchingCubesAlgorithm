using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public static class MarchingCubes
{
	private static List<Vector3> vertices = new List<Vector3>();
	private static List<int> triangles = new List<int>();
	private static List<Vector2> uvs = new List<Vector2>();
	private static Vector3 gridMin, gridMax, gridNoiseOffset;
	private static float gridCellRadius;
	private static float gridNoiseScale, gridNoiseHeight2D;
	private static double cellVisibleThreshold;
	private static CubeData.CubeConfiguration configuration;
	private static bool inverseTriangles = false;
	private static bool gridHasUpdated = false;
	private static float specificConfigRadius;

	public static List<GridCell> CreateMarchingCubesGrid(Vector3 min, Vector3 max, Vector3 noiseOffset, float cellRadius, float noiseScale, double visibleThreshold,
		CubeData.CubeConfiguration config = CubeData.CubeConfiguration.perlinNoise, float configRadius = 0.0f)
	{
		gridMin = min;
		gridMax = max;
		gridCellRadius = cellRadius;
		cellVisibleThreshold = visibleThreshold;
		gridNoiseOffset = noiseOffset;
		gridNoiseScale = noiseScale;
		configuration = config;
		specificConfigRadius = configRadius;

		vertices.Clear();
		triangles.Clear();
		uvs.Clear();

		return BuildGridCells();
	}

	public static bool TryUpdateGrid(ref List<GridCell> currentGrid, Vector3 updatePoint, bool add, float updateDelta, int range = 1)
	{
		// convert updatePoint to gridcell position
		//updatePoint = ConvertToGridPos(updatePoint);

		// find correct gridcell to update
		List<GridCell> cells = new List<GridCell>();
		// bool foundCell = false;
		float magnitude = 0f;
		for (int i = 0; i < currentGrid.Count; ++i)
		{
			GridCell currentCell = currentGrid[i];
			for (int j = 0; j < currentCell.pos.Length; ++j)
			{
				magnitude = (currentCell.pos[j] - updatePoint).magnitude;
				if (currentCell.pos[j] == updatePoint)
				{
					// cell = currentGrid[i];
					// foundCell = true;
					// if (foundCell)
					// 	break;

					if (!cells.Contains(currentGrid[i]))
						cells.Add(currentGrid[i]);
				}
				else if (magnitude <= range)
				{
					if (!cells.Contains(currentGrid[i]))
						cells.Add(currentGrid[i]);
				}
			}
			// if (cells.Count >= 8)
			// 	break;
			// if (foundCell)
			// 	break;
		}

		if (cells.Count == 0)//!foundCell)
		{
			Debug.LogWarning("No cell found!");
			return false;
		}

		float distance = Mathf.Infinity;
		float cellChangeDelta = 0f;
		// update gridcell
		if (add)
		{
			foreach (GridCell cell in cells)
			{
				for (int i = 0; i < cell.val.Length; ++i)
				{
					if (cell.val[i] >= 1.0)
					{
						cell.val[i] = 1.0;
						continue;
					}
					distance = (updatePoint - cell.pos[i]).magnitude;
					if (distance > range)
						continue;
					cellChangeDelta = updateDelta * (1f - (distance / range));
					cell.val[i] += cellChangeDelta * Time.deltaTime;
					// if (cell.pos[i] == updatePoint)
					// {
					// 	Debug.Log($"Adding to {updatePoint}, old: {cell.val[i]}, new: {cell.val[i] + .5f}");
					// 	if (cell.val[i] >= 1.0)
					// 	{
					// 		cell.val[i] = 1.0;
					// 		continue;
					// 	}
					// 	cell.val[i] += .5;
					// }
				}
			}
		}
		else
		{
			foreach (GridCell cell in cells)
			{
				for (int i = 0; i < cell.val.Length; ++i)
				{
					if (cell.val[i] <= 0.0)
					{
						cell.val[i] = 0.0;
						continue;
					}
					distance = (updatePoint - cell.pos[i]).magnitude;
					if (distance > range)
						continue;
					cellChangeDelta = updateDelta * (1f - (distance / range));
					cell.val[i] -= cellChangeDelta * Time.deltaTime;
					// if (cell.pos[i] == updatePoint)
					// {
					// 	Debug.Log($"Adding to {updatePoint}, old: {cell.val[i]}, new: {cell.val[i] - .5f}");
					// 	if (cell.val[i] <= 0.0)
					// 	{
					// 		cell.val[i] = 0.0;
					// 		continue;
					// 	}
					// 	cell.val[i] -= .5;
					// }
				}
			}
		}

		gridHasUpdated = true;

		vertices.Clear();
		triangles.Clear();
		uvs.Clear();

		return true;
	}

	public static bool TryGetMesh(List<GridCell> grid, out Mesh mesh, string name = "")
	{
		mesh = new Mesh();

		if (!gridHasUpdated)
		{
			return false;
		}

		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; //increase max vertices per mesh
		mesh.name = name == string.Empty ? "ProceduralMesh" : $"{name}_ProceduralMesh";

		IEnumerator createMesh = CreateMesh(grid, cellVisibleThreshold);
		bool done = !createMesh.MoveNext();
		while (!done)
		{
			done = !createMesh.MoveNext();
		}

		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		gridHasUpdated = false;

		return true;
	}

	public static void SetNoiseScale(float value) { if (value == gridNoiseScale) return; gridNoiseScale = value; gridHasUpdated = true; }
	public static void SetNoiseOffset(Vector3 value) { if (value == gridNoiseOffset) return; gridNoiseOffset = value; gridHasUpdated = true; }
	public static void SetGridRadius(int value) { if (value == gridCellRadius) return; gridCellRadius = value; gridHasUpdated = true; }
	public static void SetCellVisibleThreshold(float value) { if (value == cellVisibleThreshold) return; cellVisibleThreshold = value; gridHasUpdated = true; }
	public static void SetInverseTriangles(bool value) { if (value == inverseTriangles) return; inverseTriangles = value; gridHasUpdated = true; }
	public static void SetGridMin(Vector3 value) { if (value == gridMin) return; gridMin = value; gridHasUpdated = true; }
	public static void SetGridMax(Vector3 value) { if (value == gridMax) return; gridMax = value; gridHasUpdated = true; }
	public static void SetGridNoiseHeight2D(float value) { if (value == gridNoiseHeight2D) return; gridNoiseHeight2D = value; gridHasUpdated = true; }

	static Vector3 ConvertToGridPos(Vector3 pos)
	{
		return new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
	}

	static List<GridCell> BuildGridCells()
	{
		List<GridCell> grid = new List<GridCell>();

		gridHasUpdated = true;

		if (configuration == CubeData.CubeConfiguration.perlinNoise)
		{
			for (float x = gridMin.x; x < gridMax.x; x += gridCellRadius)
			{
				for (float z = gridMin.z; z < gridMax.z; z += gridCellRadius)
				{
					for (float y = gridMin.y; y < gridMax.y; y += gridCellRadius)
					{
						if (x + gridCellRadius > gridMax.x || y + gridCellRadius > gridMax.y || z + gridCellRadius > gridMax.z)
							continue;
						GridCell cell = new GridCell();
						cell.pos = new Vector3[]
						{
						new Vector3(x, y, z + gridCellRadius), //LowFrontLeft
						new Vector3(x + gridCellRadius, y, z + gridCellRadius), //LowFrontRight
						new Vector3(x + gridCellRadius, y, z), //LowBackRight
						new Vector3(x, y, z), //LowBackLeft
						
						new Vector3(x, y + gridCellRadius, z + gridCellRadius), //HighFrontLeft
						new Vector3(x + gridCellRadius, y + gridCellRadius, z + gridCellRadius), //HighFrontRight
						new Vector3(x + gridCellRadius, y + gridCellRadius, z), //HighBackRight
						new Vector3(x, y + gridCellRadius, z), //HighBackLeft						
						};
						cell.val = new double[8];

						AssignNoiseValues(cell);

						grid.Add(cell);
					}
				}
			}
		}
		else if (configuration == CubeData.CubeConfiguration.perlinNoise2D)
		{
			for (float x = gridMin.x; x < gridMax.x; x += gridCellRadius)
			{
				for (float z = gridMin.z; z < gridMax.z; z += gridCellRadius)
				{
					for (float y = gridMin.y; y < gridMax.y; y += gridCellRadius)
					{
						if (x + gridCellRadius > gridMax.x || y + gridCellRadius > gridMax.y || z + gridCellRadius > gridMax.z)
							continue;
						GridCell cell = new GridCell();
						cell.pos = new Vector3[]
						{
						new Vector3(x, y, z + gridCellRadius), //LowFrontLeft
						new Vector3(x + gridCellRadius, y, z + gridCellRadius), //LowFrontRight
						new Vector3(x + gridCellRadius, y, z), //LowBackRight
						new Vector3(x, y, z), //LowBackLeft
						
						new Vector3(x, y + gridCellRadius, z + gridCellRadius), //HighFrontLeft
						new Vector3(x + gridCellRadius, y + gridCellRadius, z + gridCellRadius), //HighFrontRight
						new Vector3(x + gridCellRadius, y + gridCellRadius, z), //HighBackRight
						new Vector3(x, y + gridCellRadius, z), //HighBackLeft						
						};
						cell.val = new double[8];

						AssignNoiseValues2D(cell);

						grid.Add(cell);
					}
				}
			}
		}
		else if (configuration == CubeData.CubeConfiguration.sphere)
		{
			for (float x = gridMin.x; x < gridMax.x; x += gridCellRadius)
			{
				for (float z = gridMin.z; z < gridMax.z; z += gridCellRadius)
				{
					for (float y = gridMin.y; y < gridMax.y; y += gridCellRadius)
					{
						if (x + gridCellRadius > gridMax.x || y + gridCellRadius > gridMax.y || z + gridCellRadius > gridMax.z)
							continue;
						GridCell cell = new GridCell();
						cell.pos = new Vector3[]
						{
						new Vector3(x, y, z + gridCellRadius), //LowFrontLeft
						new Vector3(x + gridCellRadius, y, z + gridCellRadius), //LowFrontRight
						new Vector3(x + gridCellRadius, y, z), //LowBackRight
						new Vector3(x, y, z), //LowBackLeft
						
						new Vector3(x, y + gridCellRadius, z + gridCellRadius), //HighFrontLeft
						new Vector3(x + gridCellRadius, y + gridCellRadius, z + gridCellRadius), //HighFrontRight
						new Vector3(x + gridCellRadius, y + gridCellRadius, z), //HighBackRight
						new Vector3(x, y + gridCellRadius, z), //HighBackLeft						
						};
						cell.val = new double[8];

						AssignSphereValues(cell);

						grid.Add(cell);
					}
				}
			}
		}
		else if (configuration == CubeData.CubeConfiguration.cube)
		{
			for (float x = gridMin.x; x < gridMax.x; x += gridCellRadius)
			{
				for (float z = gridMin.z; z < gridMax.z; z += gridCellRadius)
				{
					for (float y = gridMin.y; y < gridMax.y; y += gridCellRadius)
					{
						if (x + gridCellRadius > gridMax.x || y + gridCellRadius > gridMax.y || z + gridCellRadius > gridMax.z)
							continue;
						GridCell cell = new GridCell();
						cell.pos = new Vector3[]
						{
						new Vector3(x, y, z + gridCellRadius), //LowFrontLeft
						new Vector3(x + gridCellRadius, y, z + gridCellRadius), //LowFrontRight
						new Vector3(x + gridCellRadius, y, z), //LowBackRight
						new Vector3(x, y, z), //LowBackLeft
						
						new Vector3(x, y + gridCellRadius, z + gridCellRadius), //HighFrontLeft
						new Vector3(x + gridCellRadius, y + gridCellRadius, z + gridCellRadius), //HighFrontRight
						new Vector3(x + gridCellRadius, y + gridCellRadius, z), //HighBackRight
						new Vector3(x, y + gridCellRadius, z), //HighBackLeft						
						};
						cell.val = new double[8];

						AssignCubeValues(cell);

						grid.Add(cell);
					}
				}
			}
		}

		return grid;
	}

	static void AssignNoiseValues(GridCell cell)
	{
		for (int i = 0; i < cell.val.Length; i++)
		{
			cell.val[i] = GetNoiseValue(cell.pos[i], gridNoiseOffset, gridNoiseScale);
		}
	}

	static void AssignNoiseValues2D(GridCell cell)
	{
		for (int i = 0; i < cell.val.Length; i++)
		{
			cell.val[i] = GetNoiseValue2D(cell.pos[i], gridNoiseOffset, (gridMax - gridMin), gridNoiseScale, gridNoiseHeight2D);
		}
	}

	static void AssignSphereValues(GridCell cell)
	{
		for (int i = 0; i < cell.val.Length; i++)
		{
			if (Vector3.Distance(cell.pos[i], (gridMin + gridMax) / 2f) < (specificConfigRadius <= float.Epsilon ? 2f : specificConfigRadius))
				cell.val[i] = 0;
			else
				cell.val[i] = 1;
		}
	}

	static void AssignCubeValues(GridCell cell)
	{
		for (int i = 0; i < cell.val.Length; i++)
		{
			Vector3 differenceVector = (cell.pos[i] - ((gridMin + gridMax) / 2f));
			Vector3 distance = specificConfigRadius <= float.Epsilon ? new Vector3(2.5f, 2.5f, 2.5f) : Vector3.one * specificConfigRadius;
			if (differenceVector.x < -distance.x || differenceVector.y < -distance.x || differenceVector.z < -distance.x ||
				differenceVector.x > distance.x || differenceVector.y > distance.x || differenceVector.z > distance.x)
				cell.val[i] = 1;
			else
				cell.val[i] = 0;
		}
	}

	static IEnumerator CreateMesh(List<GridCell> grid, double cellVisibleThreshold)
	{
		for (int i = 0; i < grid.Count; i++)
		{
			GridCell cell = grid[i];

			int config = GetCubeConfig(cell, cellVisibleThreshold);

			if (CubeData.edgeTable[config] == 0)
				continue;

			IsoFace(config, cell);
			yield return null;
		}
	}

	static int GetCubeConfig(GridCell cell, double cellVisibleThreshold)
	{
		// config code based on current cube on points
		int config = 0;

		config += cell.val[0] < cellVisibleThreshold ? (1 << 0) : 0; //alt: 0x0 (hex) or 0b0 (binary)
		config += cell.val[1] < cellVisibleThreshold ? (1 << 1) : 0; //alt: 0x1 or 0b1
		config += cell.val[2] < cellVisibleThreshold ? (1 << 2) : 0; //alt: 0x2 or 0b10
		config += cell.val[3] < cellVisibleThreshold ? (1 << 3) : 0; //alt: 0x3 or 0b11
		config += cell.val[4] < cellVisibleThreshold ? (1 << 4) : 0; //alt: 0x4 or 0b100
		config += cell.val[5] < cellVisibleThreshold ? (1 << 5) : 0; //alt: 0x5 or 0b101
		config += cell.val[6] < cellVisibleThreshold ? (1 << 6) : 0; //alt: 0x6 or 0b110
		config += cell.val[7] < cellVisibleThreshold ? (1 << 7) : 0; //alt: 0x7 or 0b111

		return config;
	}

	static void IsoFace(int config, GridCell cell)
	{
		cell.vertices = new Vector3[12];

		//vertices
		int beforeCount = vertices.Count;
		/* Find the vertices where the surface intersects the cube */
		if ((CubeData.edgeTable[config] & 1) != 0)
			cell.vertices[0] = (InterpolateVerts(cellVisibleThreshold, cell.pos[0], cell.pos[1], cell.val[0], cell.val[1]));
		if ((CubeData.edgeTable[config] & 2) != 0)
			cell.vertices[1] = (InterpolateVerts(cellVisibleThreshold, cell.pos[1], cell.pos[2], cell.val[1], cell.val[2]));
		if ((CubeData.edgeTable[config] & 4) != 0)
			cell.vertices[2] = (InterpolateVerts(cellVisibleThreshold, cell.pos[2], cell.pos[3], cell.val[2], cell.val[3]));
		if ((CubeData.edgeTable[config] & 8) != 0)
			cell.vertices[3] = (InterpolateVerts(cellVisibleThreshold, cell.pos[3], cell.pos[0], cell.val[3], cell.val[0]));
		if ((CubeData.edgeTable[config] & 16) != 0)
			cell.vertices[4] = (InterpolateVerts(cellVisibleThreshold, cell.pos[4], cell.pos[5], cell.val[4], cell.val[5]));
		if ((CubeData.edgeTable[config] & 32) != 0)
			cell.vertices[5] = (InterpolateVerts(cellVisibleThreshold, cell.pos[5], cell.pos[6], cell.val[5], cell.val[6]));
		if ((CubeData.edgeTable[config] & 64) != 0)
			cell.vertices[6] = (InterpolateVerts(cellVisibleThreshold, cell.pos[6], cell.pos[7], cell.val[6], cell.val[7]));
		if ((CubeData.edgeTable[config] & 128) != 0)
			cell.vertices[7] = (InterpolateVerts(cellVisibleThreshold, cell.pos[7], cell.pos[4], cell.val[7], cell.val[4]));
		if ((CubeData.edgeTable[config] & 256) != 0)
			cell.vertices[8] = (InterpolateVerts(cellVisibleThreshold, cell.pos[0], cell.pos[4], cell.val[0], cell.val[4]));
		if ((CubeData.edgeTable[config] & 512) != 0)
			cell.vertices[9] = (InterpolateVerts(cellVisibleThreshold, cell.pos[1], cell.pos[5], cell.val[1], cell.val[5]));
		if ((CubeData.edgeTable[config] & 1024) != 0)
			cell.vertices[10] = (InterpolateVerts(cellVisibleThreshold, cell.pos[2], cell.pos[6], cell.val[2], cell.val[6]));
		if ((CubeData.edgeTable[config] & 2048) != 0)
			cell.vertices[11] = (InterpolateVerts(cellVisibleThreshold, cell.pos[3], cell.pos[7], cell.val[3], cell.val[7]));

		vertices.AddRange(cell.vertices);

		cell.triangles = new int[CubeData.triangleTable[config].Length * 3];
		for (int i = 0; CubeData.triangleTable[config][i] != -1; i += 3)
		{
			cell.triangles[i] = (CubeData.triangleTable[config][i]);
			cell.triangles[i + 1] = (CubeData.triangleTable[config][i + 1]);
			cell.triangles[i + 2] = (CubeData.triangleTable[config][i + 2]);
		}

		if (inverseTriangles)
		{
			for (int i = 0; i < cell.triangles.Length; i++)
			{
				triangles.Add(beforeCount + cell.triangles[i]);
			}
		}
		else
		{
			for (int i = cell.triangles.Length - 1; i >= 0; i--)
			{
				triangles.Add(beforeCount + cell.triangles[i]);
			}
		}

		//triangles + uvs
		int addedCount = vertices.Count - beforeCount;
		int addedIndex = 0;
		bool adc = true;
		for (int i = addedCount; i > 0; i--)
		{
			if (adc)
			{
				switch (addedIndex)
				{
					case 0:
						uvs.Add(UVCoord.A);
						break;
					case 1:
						uvs.Add(UVCoord.C);
						break;
					case 2:
						uvs.Add(UVCoord.D);
						break;
				}
			}
			else
			{
				switch (addedIndex)
				{
					case 0:
						uvs.Add(UVCoord.A);
						break;
					case 1:
						uvs.Add(UVCoord.B);
						break;
					case 2:
						uvs.Add(UVCoord.C);
						break;
				}
			}
			addedIndex++;
			addedIndex %= 3;
			adc = addedIndex == 0 ? !adc : adc;
		}
	}

	static Vector3 InterpolateVerts(double cellVisibleThreshold, Vector3 p1, Vector3 p2, double valp1, double valp2)
	{
		float t = (float)((cellVisibleThreshold - valp1) / (valp2 - valp1));
		return p1 + t * (p2 - p1);
	}

	static double GetNoiseValue(Vector3 pos, Vector3 offset, float gridNoiseScale)
	{
		double AB = Mathf.PerlinNoise(offset.x + gridNoiseScale * pos.x, offset.y + gridNoiseScale * pos.y);         // get all three(3) permutations of noise for x,y and z
		double BC = Mathf.PerlinNoise(offset.y + gridNoiseScale * pos.y, offset.z + gridNoiseScale * pos.z);
		double AC = Mathf.PerlinNoise(offset.x + gridNoiseScale * pos.x, offset.z + gridNoiseScale * pos.z);

		double BA = Mathf.PerlinNoise(offset.y + gridNoiseScale * pos.y, offset.x + gridNoiseScale * pos.x);         // and their reverses
		double CB = Mathf.PerlinNoise(offset.z + gridNoiseScale * pos.z, offset.y + gridNoiseScale * pos.y);
		double CA = Mathf.PerlinNoise(offset.z + gridNoiseScale * pos.z, offset.x + gridNoiseScale * pos.x);

		double ABC = (AB + BC + AC + BA + CB + CA) / 6.0;    // and return the average
		return ABC;
	}

	static double Perlin2D(float nx, float ny, float nz)
	{
		return Mathf.PerlinNoise(nx, nz) * ny;
	}
	static double GetNoiseValue2D(Vector3 pos, Vector3 offset, Vector3 gridSize, float gridNoiseScale, float noiseHeight)
	{
		float nx = (offset.x + pos.x / gridSize.x) * gridNoiseScale;
		float ny = (pos.y * (1f / noiseHeight) / gridSize.y) * gridNoiseScale;
		float nz = (offset.z + pos.z / gridSize.z) * gridNoiseScale;
		return Perlin2D(nx, ny, nz);
	}
}