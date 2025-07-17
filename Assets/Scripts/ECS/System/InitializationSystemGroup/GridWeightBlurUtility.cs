using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public static class GridWeightBlurUtility
{
    [BurstCompile]
    public static void BlurPenaltyMap(ref NativeArray<Node> grid, int width, int height, int blurSize)
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        var penaltiesHorizontalPass = new NativeArray<int>(width * height, Allocator.Temp);
        var penaltiesVerticalPass = new NativeArray<int>(width * height, Allocator.Temp);

        try
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = -kernelExtents; x <= kernelExtents; x++)
                {
                    int sampleX = math.clamp(x, 0, kernelExtents);
                    penaltiesHorizontalPass[y * width] += grid[y * width + sampleX].movementPenalty;
                }

                for (int x = 1; x < width; x++)
                {
                    int removeIndex = math.clamp(x - kernelExtents - 1, 0, width);
                    int addIndex = math.clamp(x + kernelExtents, 0, width - 1);

                    int horizontalPassIndex = y * width + x;
                    penaltiesHorizontalPass[horizontalPassIndex] = penaltiesHorizontalPass[horizontalPassIndex - 1] -
                                                                   grid[y * width + removeIndex].movementPenalty +
                                                                   grid[y * width + addIndex].movementPenalty;
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = -kernelExtents; y <= kernelExtents; y++)
                {
                    int sampleY = math.clamp(y, 0, kernelExtents);
                    penaltiesVerticalPass[x] += penaltiesHorizontalPass[sampleY * width + x];
                }

                int blurredPenalty = (int)math.round((float)penaltiesVerticalPass[x] / (kernelSize * kernelSize));
                var node = grid[x];
                node.movementPenalty = blurredPenalty;
                grid[x] = node;

                for (int y = 1; y < height; y++)
                {
                    int removeIndex = math.clamp(y - kernelExtents - 1, 0, height);
                    int addIndex = math.clamp(y + kernelExtents, 0, height - 1);
                    
                    int verticalPassIndex = y * width + x;
                    penaltiesVerticalPass[verticalPassIndex] = penaltiesVerticalPass[verticalPassIndex - width] - 
                                                               penaltiesHorizontalPass[removeIndex * width + x] + 
                                                               penaltiesHorizontalPass[addIndex * width + x];
                    
                    blurredPenalty = (int)math.round((float)penaltiesVerticalPass[verticalPassIndex] / (kernelSize * kernelSize));
                    
                    node = grid[verticalPassIndex];
                    node.movementPenalty = blurredPenalty;
                    grid[verticalPassIndex] = node;
                }
            }
        }
        finally
        {
            if (penaltiesHorizontalPass.IsCreated) penaltiesHorizontalPass.Dispose();
            if (penaltiesVerticalPass.IsCreated) penaltiesVerticalPass.Dispose();
        }
    }
}