# Data Extraction Guide (WIP)

This is a guide designed to help any one looking to extract data from the game. This is accomplished by a custom mod that when enabled in the game will read and export game data.
In order the extract the data you want, you will need to have your computer prepared to do Captain of Industry modding. Also some knowledge of programming languages will help as part of the process involves reading through C# and writting and workign with simple C#.

## 0. Before Starting

### Environment Setup

The best way to get started is to follow the guide [here](https://github.com/MaFi-Games/Captain-of-industry-modding) on how to get started with modding.

The linked guide should help you setup Visual Studio and build the sample mod with the sample code. If you can get the sample mod build and running in the game, you are almost ready.

You will also need a program called JetBrains dotPeek to explore the original game code.

![image](https://user-images.githubusercontent.com/1429949/176127507-6a119d84-1b53-42f1-a469-54fb1a5fc3fc.png)

In dotPeek, simply bring up the Assembly Explorer and click on "Explore Folder", select the "Managed Folder" in Captain of Industry_Data folder.

Once here you will be able to explore the game code. Most of the revelant code will be under these namespaces:

* Mafi
* Mafi.Base
* Mafi.Core

## 1 - First Step - Identify Data to Extract

Before starting you will want to have a general idea of what you would like to extract. In this example, we will try to find some of the research costs in the technology tree.

### Protos (aka Prototypes)

C# is an object oriented language, meaning that everything in the game inherits it's properties from some base Class within the code, this is its "Proto" or Prototype.

This is what you want to track down, and this is what you need to explore the game's code to find out.

There are many ways to identify an item's Proto, but I will share the main two I use.

#### A - Looking in Mafi.Base.Ids

Every item in the game has a unique ID, all of these IDs are located in the same file/namespace/assembly. **Mafi.Base.Ids**

Looking through the file, we can eventually find the Ids in: **Mafi.Base.Ids.Research**

![image](https://user-images.githubusercontent.com/1429949/176129870-a6146bda-0870-4b62-93e5-afa97ca34324.png)

You can see here that we have found that for the research items the Proto is called: **ResearchNodeProto**

#### B - Looking in Data Files

So the Protos are the Classes that the items will inherit, but where does that take place? That is in the Data Files.

![image](https://user-images.githubusercontent.com/1429949/176130562-1cc42b5a-4015-40b1-bff0-22c646306bee.png)

Looking through **Mafi.Base.Prototypes** we can see all the files where the items are defined. If we open and read through the code, we will be able to see what types of Protos are being registered for what Items.

![image](https://user-images.githubusercontent.com/1429949/176131116-4397bbe1-34d1-4841-8dea-b67a728b4405.png)

We can see the Farming Research item here being registered with the ResearchNodeProtoBuilder, we can deduce from this that the Proto we are after is most likely: **ResearchNodeProto** 

## 2 - Begin Extraction Code

Now that we know what the proto is **ResearchNodeProto** , we can start writting some code the get the game to spit out the details. This is where we start in Visual Studio

NOTE: Reading through the source code of the modfile in this repo, you can see examples of how the following is used, an djust copy and paste things.

## 3 - Getting The Protos From the ProtoDB

All the Protos are registed in the game with the ProtosDB, we can access this from the mod and ask it to give us all the instances of a certain Proto.

`IEnumerable<ResearchNodeProto> researchNodes = protosDb.All<ResearchNodeProto>();`

So now we can start looking through the instances to read the data from them.

    IEnumerable<ResearchNodeProto> researchNodes = protosDb.All<ResearchNodeProto>();
    foreach (ResearchNodeProto researchNode in researchNodes)
    {
    
    }
    
## 4 - Reading Data From The Protos

This part is simplied by using Visual Studio, we will rely on the code analysis and auto complete feature to easily explore our objects.

![image](https://user-images.githubusercontent.com/1429949/176133388-8a1fcd71-da55-4937-a99c-9edeb3ecd842.png)

By starting to type, we can thank Visual Studio for showing us this Item's properties. This is the data we want.

Now, to make sure this data is usable, lets use the Loggin system, to extract some of the properties we can see.

NOTE: Many of the values will be numbers or other types, since we want to extract to text files, we will need to use the `.ToString()` method on these items to get the text.

Occasionaly you might find values that are arrays, we will need to also loop through these to extract the full data.
![image](https://user-images.githubusercontent.com/1429949/176134432-3590fe7d-3e7a-4bae-a1f3-b9a0479ddf4a.png)

This is what we should have so far. 

    IEnumerable<ResearchNodeProto> researchNodes = protosDb.All<ResearchNodeProto>();

    foreach (ResearchNodeProto researchNode in researchNodes)
    {

        Log.Info("Difficulty : " + researchNode.Difficulty.ToString());

        Log.Info("TotalStepsRequired : " + researchNode.TotalStepsRequired.ToString());

        Log.Info("Units : " + researchNode.Units.ToString());

        foreach (TechnologyProto requiredProto in researchNode.RequiredUnlockedProtos)
        {
            Log.Info("RequiredProto Name:  " + requiredProto.Strings.Name.ToString());
        }
    
    }


With this we should be able to spit out some data.

## 5 - Building The Mod And Checking Logs

Now we can build the MOD file from Visual Studio. If you followed the official MOD Guide, you should be able to get the code you just build into the game and running.

NOTE: Make sure MODS are enabled, and **Always** use CAPTAIN difficulty for exporting data, since different difficulties might affect the output.

The log files should start showing the data once the game boots. You can find the log files in your local My Documents folder under Captain of Industry

![image](https://user-images.githubusercontent.com/1429949/176136394-d5183e0d-fcbe-4078-b8cc-ef0a6bd4c2bd.png)

We can see now in the log files that we have a good start with the data, but we can spot somethings we missed and some issues.

![image](https://user-images.githubusercontent.com/1429949/176176240-d7fa5914-ce85-48e4-a897-c4d62a8c0024.png)

1. Its a bit diffcult to identify each item, so we can add some markup to separate each item.
2. We did not identify the actual item, so we should add the Name or the Id so we can know what we are looking at
3. We had the wrong Type for "Units". The code we wrote thinks it was a string but it was actually another array.

## 6 - Iteration, Trial and Error

So basically from now on, it is just repeating steps 4, 5 and 6.

1. Exploring the fields/values/properties our Proto has.
2. Writting code to print out the values as strings
3. Building the latest mod code
4. Running a new game, and reading the logs.

Lets update our code to fix the issues we have and try again.

This is what our updated code will look like:

![image](https://user-images.githubusercontent.com/1429949/176138780-d5e738e8-24d6-43ef-abc4-4abb861c8e43.png)

And after reviewing the logs, we can see we are getting closer.

![image](https://user-images.githubusercontent.com/1429949/176140032-7bad80d4-4fda-4be2-aea6-edd437341a8a.png)

We can compare to the Ingame Data to see we are on the right track.

![image](https://user-images.githubusercontent.com/1429949/176140110-1b47e191-4693-4a1a-ae18-768f991a6d82.png)

We can see that "nodeUnit" is what we unlock, based on the logged code:

![image](https://user-images.githubusercontent.com/1429949/176140341-2eae641e-213c-4cf7-8f09-cb6f1a2d0654.png)

We know it exports:

* 2 LayoutEntityUnlock
* 3 VehicleUnlock
* 2 RecipeUnlock

We can verify this is correct in the game, but we want to actually get a bit more detail, so we need to once again tweak the code.











