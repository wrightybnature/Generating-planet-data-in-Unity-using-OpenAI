using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

// Uses the PlanetNameGenerator script's names to generate descriptions for each planet
public class PlanetDescriptionGenerator : MonoBehaviour
{
    public PlanetNameGenerator planetNameGenerator;

    private OpenAIAPI api;
    public TMP_Text[] descriptionTexts;

    void Start()
    {
        api = new OpenAIAPI(Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User)); // API key setup

        planetNameGenerator.onNamesGenerated += OnPlanetNamesGenerated;
    }

    private void OnDestroy()
    {
        if (planetNameGenerator != null)
        {
            planetNameGenerator.onNamesGenerated -= OnPlanetNamesGenerated;
        }
    }

    // Add more if more planets are needed
    private void OnPlanetNamesGenerated()
    {
        List<string> planetNames = new List<string>
        {
        planetNameGenerator.planetName1,
        planetNameGenerator.planetName2,
        planetNameGenerator.planetName3
        };

        StartGeneration(planetNames);
    }

    public void StartGeneration(List<string> planetNames)
    {
        StartCoroutine(GenerateDescriptionsCoroutine(planetNames));
    }

    // 
    private IEnumerator GenerateDescriptionsCoroutine(List<string> planetNames)
    {
        for (int i = 0; i < planetNames.Count; i++)
        {
            var planetName = planetNames[i];
            var task = GeneratePlanetDetails(planetName);
            while (!task.IsCompleted)
            {
                yield return null; 
            }

            var (rawResponse, details) = task.Result;

            if (i < descriptionTexts.Length)
            {
                DisplayPlanetDetails(rawResponse, descriptionTexts[i]);
            }
        }
    }

    // Use the planet name generated previously to create a description with the following details and formatted in the following way
    private async Task<(string, PlanetDetails)> GeneratePlanetDetails(string planetName)
    {
        var prompt = $"Create a detailed description for a planet named {planetName}. " +
             "Format the details as follows: \n" +
             "Provide a unique detail about the planet in one sentence at the end.\n" +
             "- Temperature: [Temperature in Celsius]\n" +
             "- Hospitable: [Yes/No]\n" +
             "- Water: [Yes/No]\n" +
             "- Breathable Atmosphere: [Yes/No]\n" +
             "- Gravity: [Gravity in Newtons between 3 and 20]\n" +
             "For example: \n" +
             $"{planetName} has been a cold planet enveloped in ice for thousands of years - not that that's stopped any adventurers from going near it!\n" +
             "- Temperature: -20\n" +
             "- Hospitable: Yes\n" +
             "- Water: Yes\n" +
             "- Breathable Atmosphere: Yes\n" +
             "- Gravity: 9.8\n";

        // Variables controlling the creativity, length and model used for the API's response
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 0.5,
            MaxTokens = 150,
            TopP = 1,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            Messages = new List<ChatMessage>
            {
                new ChatMessage(ChatMessageRole.System, prompt) // Sending all info to API
            }
        });

        var response = chatResult.Choices[0].Message.Content;
        Debug.Log("Raw API Response: " + response); // Checking response

        var details = ParsePlanetDetails(response);

        return (response, details);
    }

    // Regex cleanup and variable assigning/finding
    private PlanetDetails ParsePlanetDetails(string response)
    {
        var details = new PlanetDetails();
        var lines = response.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var tempPattern = new Regex(@"Temperature: (-?\d+)");
        var hospitablePattern = new Regex(@"Hospitable: (Yes|No)");
        var waterPattern = new Regex(@"Water: (Yes|No)");
        var atmospherePattern = new Regex(@"Breathable Atmosphere: (Yes|No)");
        var gravityPattern = new Regex(@"Gravity: (\d+(\.\d+)?)N");
        

        foreach (var line in lines)
        {
            var tempMatch = tempPattern.Match(line);
            if (tempMatch.Success)
                details.Temperature = int.Parse(tempMatch.Groups[1].Value);

            var hospitableMatch = hospitablePattern.Match(line);
            if (hospitableMatch.Success)
                details.Hospitable = hospitableMatch.Groups[1].Value == "Yes";

            var waterMatch = waterPattern.Match(line);
            if (waterMatch.Success)
                details.Water = waterMatch.Groups[1].Value == "Yes";

            var atmosphereMatch = atmospherePattern.Match(line);
            if (atmosphereMatch.Success)
                details.BreathableAtmosphere = atmosphereMatch.Groups[1].Value == "Yes";

            var gravityMatch = gravityPattern.Match(line);
            if (gravityMatch.Success)
                details.Gravity = float.Parse(gravityMatch.Groups[1].Value);
        }

        details.Description = lines[0];

        return details;
    }

    private void DisplayPlanetDetails(string rawResponse, TMP_Text targetTextComponent)
    {
        if (targetTextComponent != null)
        {
            targetTextComponent.text = rawResponse;
        }
    }

    public struct PlanetDetails
    {
        public string Description;
        public int Temperature;
        public bool Hospitable;
        public bool Water;
        public bool BreathableAtmosphere;
        public float Gravity;

    }
}