using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanetNameGenerator : MonoBehaviour
{
    private OpenAIAPI api;
    private List<ChatMessage> messages;

    public TMP_Text[] planetTexts;
    
    public delegate void OnNamesGenerated();
    public event OnNamesGenerated onNamesGenerated;

    public string planetName1;
    public string planetName2;
    public string planetName3;

    void Start()
    {
        api = new OpenAIAPI(Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User));

        GeneratePlanetNames();
    }

    // Explaining to API what to do
    private async void GeneratePlanetNames()
    {
        messages = new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRole.System, "You will be provided with a description and seed words, and your task is to generate planet names."),
            new ChatMessage(ChatMessageRole.User, "Description: Interesting planet in space.\nSeed words: cold, dry, life, icy, hot, acidic, sandy, lifeless, forest, water, ocean, fire, dust, technology, aliens, cyberpunk, lava, tectonic, earth, moon, sun, gaseous, goldilocks zone.")
        };

        // Variables that change the generated response's creativity, length, and which model is used
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 1.5,
            MaxTokens = 256,
            TopP = 1,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            Messages = messages
        });

        var planetContent = chatResult.Choices[0].Message.Content; // For holding the API's response

        var planetLines = planetContent.Split('\n'); // I knew the output would have each planet name on a separate line because I used OpenAI's API to fine-tune the response a bit
        List<string> cleanedNames = new List<string>();

        foreach (var line in planetLines)
        {
            var potentialName = line.Split(' ')[1];

            if (Regex.IsMatch(potentialName, @"^[A-Za-z'-]+$")) // Getting rid of unecessary characters
            {
                cleanedNames.Add(potentialName);
            }
        }

        // Assigning the now clean generated names to variables that can be used in game
        if(cleanedNames.Count >= 3)
        {
            planetName1 = cleanedNames[0];
            planetTexts[0].text = planetName1;

            planetName2 = cleanedNames[1];
            planetTexts[1].text = planetName2;

            planetName3 = cleanedNames[2];
            planetTexts[2].text = planetName3;
        }
        
        onNamesGenerated?.Invoke();

        // Saving new planet names to file
        var filePath = Path.Combine(Application.persistentDataPath, "PlanetNames.txt");
        File.WriteAllLines(filePath, cleanedNames);
    }

    public void SaveGeneratedPlanetNames(string name1, string name2, string name3)
    {
        planetName1 = name1;
        planetName2 = name2;
        planetName3 = name3;
    }
}