using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;

namespace Bullet
{

public class BulletManager : MonoBehaviour
{
	private const float limit = 100.0f;
	private const float maxRadius = 3.0f;
	public struct Circle
	{
		public float2 position;
		public float radius;
	}

	public NativeArray<Circle> bulletCircles;
	public NativeArray<Circle> enemyCircles;
	
	public NativeArray<int> enemiesHitCount;

	BulletUpdateJob2 updateJob;
	JobHandle jobHandle;
	
    void Start()
    {
        bulletCircles = new NativeArray<Circle>(500, Allocator.Persistent);
        enemyCircles = new NativeArray<Circle>(500, Allocator.Persistent);
        enemiesHitCount = new NativeArray<int>(500, Allocator.Persistent);
        
		
		var random = Unity.Mathematics.Random.CreateFromIndex(0);
		for(int i = 0; i < 500; i++)
		{
			bulletCircles[i] = new Circle()
			{
				position = new float2(random.NextFloat(-limit, limit), random.NextFloat(-limit, limit)),
				radius = random.NextFloat(maxRadius)
			};
			enemyCircles[i] = new Circle()
			{
				position = new float2(random.NextFloat(-limit, limit), random.NextFloat(-limit, limit)),
				radius = random.NextFloat(maxRadius)
			};
		}
		
		updateJob = new BulletUpdateJob2
		{
			BulletCircles = bulletCircles,
			EnemyCircles = enemyCircles,
			EnemiesHitCount = enemiesHitCount
		};
    }

	void OnDestroy()
	{
		bulletCircles.Dispose();
		enemyCircles.Dispose();
		enemiesHitCount.Dispose();
	}

	void Update()
	{
		jobHandle = updateJob.Schedule(enemiesHitCount.Length, 32);
	}

	void LateUpdate()
	{
        jobHandle.Complete();
	}


	[BurstCompile]
	private struct BulletUpdateJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<Circle> BulletCircles;
		[ReadOnly]
		public NativeArray<Circle> EnemyCircles;

		[WriteOnly]
		public NativeArray<int> EnemiesHitCount;

		public void Execute(int enemyIndex)
		{
			EnemiesHitCount[enemyIndex] = 0;
			for (int i = 0; i < BulletCircles.Length; i++)
			{
				var delta = BulletCircles[i].position - EnemyCircles[enemyIndex].position;
				var radius = BulletCircles[i].radius + EnemyCircles[enemyIndex].radius;
				if(math.lengthsq(delta) < radius * radius)
				{
					EnemiesHitCount[enemyIndex]++;
				}
			}
			
		}
	}
	
	[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
	private struct BulletUpdateJob2 : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<Circle> BulletCircles;
		[ReadOnly]
		public NativeArray<Circle> EnemyCircles;

		public NativeArray<int> EnemiesHitCount;

		public void Execute(int enemyIndex)
		{
			EnemiesHitCount[enemyIndex] = 0;
			for (int i = 0; i < BulletCircles.Length; i++)
			{
				var delta = BulletCircles[i].position - EnemyCircles[enemyIndex].position;
				var radius = BulletCircles[i].radius + EnemyCircles[enemyIndex].radius;
				EnemiesHitCount[enemyIndex] += Convert.ToInt32(math.lengthsq(delta) < radius * radius);
			}
			
		}
	}

}
}