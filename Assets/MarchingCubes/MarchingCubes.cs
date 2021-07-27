using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class MarchingCubes
{
	private static List<Vector3> vertices = new List<Vector3>();
	private static List<int> triangles = new List<int>();
	private static List<Vector2> uvs = new List<Vector2>();
	private static Vector3 gridMin, gridMax, gridNoiseOffset;
	private static float gridCellRadius, gridNoiseScale;
	private static double cellVisibleThreshold;
	private static CubeData.CubeConfiguration configuration;
	private static bool inverseTriangles = false;
	private static bool gridHasUpdated = false;

	public static List<GridCell> GetCreateGrid(Vector3 min, Vector3 max, Vector3 noiseOffset, float cellRadius, float noiseScale, double visibleThreshold,
		CubeData.CubeConfiguration config = CubeData.CubeConfiguration.perlinNoise)
	{
		gridMin = min;
		gridMax = max;
		gridCellRadius = cellRadius;
		cellVisibleThreshold = visibleThreshold;
		gridNoiseOffset = noiseOffset;
		gridNoiseScale = noiseScale;
		configuration = config;

		vertices.Clear();
		triangles.Clear();
		uvs.Clear();

		return BuildGridCells();
	}

	public static bool GetUpdatedGrid(out List<GridCell> grid)
	{
		grid = new List<GridCell>();

		if (!gridHasUpdated)
		{
			return false;
		}

		vertices.Clear();
		triangles.Clear();
		uvs.Clear();

		grid = BuildGridCells();

		return true;
	}

	public static bool TryGetMesh(List<GridCell> grid, out Mesh mesh)
	{
		mesh = new Mesh();

		if (!gridHasUpdated)
		{
			return false;
		}

		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; //increase max vertices per mesh
		mesh.name = "ProceduralMesh";

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
	public static void SetGridRadius(float value) { if (value == gridCellRadius) return; gridCellRadius = value; gridHasUpdated = true; }
	public static void SetCellVisibleThreshold(float value) { if (value == cellVisibleThreshold) return; cellVisibleThreshold = value; gridHasUpdated = true; }
	public static void SetInverseTriangles(bool value) { if (value == inverseTriangles) return; inverseTriangles = value; gridHasUpdated = true; }
	public static void SetGridMin(Vector3 value) { if (value == gridMin) return; gridMin = value; gridHasUpdated = true; }
	public static void SetGridMax(Vector3 value) { if (value == gridMax) return; gridMax = value; gridHasUpdated = true; }

	public static void SetCubeVertex(ref List<GridCell> grid, Vector3 position, double value)
	{
		Vector3Int pos = position.ToVector3Int();
		GridCell cell = null;
		int valueIndex = -1;
		bool found = false;
		for (int i = 0; i < grid.Count; i++)
		{
			for (int j = 0; j < grid[i].pos.Length; j++)
			{
				if (grid[i].pos[j] == pos)
				{
					cell = grid[i];
					valueIndex = j;
					found = true;
				}
				if (found)
					break;
			}
			if (found)
				break;
		}

		cell.val[valueIndex] = value;
	}

	static List<GridCell> BuildGridCells()
	{
		List<GridCell> grid = new List<GridCell>();

		gridHasUpdated = true;

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
					if (configuration == CubeData.CubeConfiguration.perlinNoise)
					{
						for (int i = 0; i < cell.val.Length; i++)
						{
							cell.val[i] = GetNoiseValue(cell.pos[i], gridNoiseOffset, gridNoiseScale);
						}
					}
					else if (configuration == CubeData.CubeConfiguration.sphere)
					{
						for (int i = 0; i < cell.val.Length; i++)
						{
							if (Vector3.Distance(cell.pos[i], (gridMin + gridMax) / 2f) < 2f)
								cell.val[i] = 1;
							else
								cell.val[i] = 0;
						}
					}
					grid.Add(cell);
				}
			}
		}
		return grid;
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

	/*
   Linearly interpolate the position where an isosurface cuts
   an edge between two vertices, each with their own scalar value
*/
	static Vector3 VertexInterp(double cellVisibleThreshold, Vector3 p1, Vector3 p2, double valp1, double valp2)
	{
		double mu;
		Vector3 p;

		if (Math.Abs(cellVisibleThreshold - valp1) < 0.00001)
			return (p1);
		if (Math.Abs(cellVisibleThreshold - valp2) < 0.00001)
			return (p2);
		if (Math.Abs(valp1 - valp2) < 0.00001)
			return (p1);
		mu = (cellVisibleThreshold - valp1) / (valp2 - valp1);
		p.x = p1.x + (float)mu * (p2.x - p1.x);
		p.y = p1.y + (float)mu * (p2.y - p1.y);
		p.z = p1.z + (float)mu * (p2.z - p1.z);

		return (p);
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
}