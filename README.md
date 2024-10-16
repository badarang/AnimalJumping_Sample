# **<애니멀 점핑!> Key Components Overview**

### **BlockGroup**
<img width="286" alt="BlockGroup" src="https://github.com/user-attachments/assets/b3ed6ec7-6d17-4e82-bd36-a0fff2683cb5">
A group of 8 blocks that handles various functionalities, such as `visibilityCheck` for optimization, two types of spawn methods (`OnSpawn` and `InitChildBlockType`), and interaction with obstacles.

### **BlockGroupDatabase**
<img width="337" alt="BlockGroupPrefabDataBase" src="https://github.com/user-attachments/assets/6a7a88a5-2e8d-49c6-817c-66b27c800aa9">
A ScriptableObject (SO) that manages a list of block group prefabs.

![BlockDistribution](https://github.com/user-attachments/assets/b790b02c-5533-4ad1-9e24-bb2f4c14b236)
You can configure the spawn probabilities of blocks for both Stage Mode and Endless Mode directly in the Inspector.

### **BlockShaderController**
Attached to individual blocks, this component manages the material based on the block's specific type.

### **BlockType**
Initializes the type for each block in the game.

### **BreakableBlock**
Each block includes this feature, which controls the interactions specific to its type.

### **GachaGame**
Implements gacha mechanics by receiving gacha information from the GameManager and executing gacha motions.

### **ObjectPool**
A basic pooling system for managing and recycling `BlockGroup` instances efficiently.
