using System;
public interface ILayoutGenerator
{
    DungeonLayout GenerateLayout(DungeonGenSettings settings);
}
