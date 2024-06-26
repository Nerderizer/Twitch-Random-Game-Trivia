using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Timers;
using System.Collections.Generic;

public class CPHInline
{
    private HttpClient client = new HttpClient();
    private string correctAnswer;
    private Timer timer;
    private Timer reminderTimer;
    private string redeemer;
    private int timerCount = 0;
    private HashSet<string> guessedUsers;

    public bool Execute()
    {
        try
        {
            // Initialize the guessed users HashSet
            guessedUsers = new HashSet<string>();

            // Get the user who redeemed the Channel Point Redemption
            redeemer = args["user"].ToString();
            string url = CPH.GetGlobalVar<string>("openTriviaUrl");

            // Fetch a random trivia question from the Open Trivia Database
            HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                string triviaData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                JObject triviaJson = JObject.Parse(triviaData);

                // Extract the question, correct answer, and incorrect answers
                string question = HttpUtility.HtmlDecode(triviaJson["results"][0]["question"].ToString());
                correctAnswer = HttpUtility.HtmlDecode(triviaJson["results"][0]["correct_answer"].ToString());
                JArray incorrectAnswers = (JArray)triviaJson["results"][0]["incorrect_answers"];
                List<string> allAnswers = incorrectAnswers.ToObject<List<string>>();
                allAnswers.Add(correctAnswer);

                // Randomize the order of the answers
                Random rng = new Random();
                int n = allAnswers.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    string value = allAnswers[k];
                    allAnswers[k] = allAnswers[n];
                    allAnswers[n] = value;
                }

                // Get the trivia timer duration and convert it to minutes and seconds
                double triviaTimer = CPH.GetGlobalVar<double>("triviaTimer");
                int triviaTimerSeconds = (int)(triviaTimer / 1000);
                int minutes = triviaTimerSeconds / 60;
                int seconds = triviaTimerSeconds % 60;

                // Send the question and answers to the chat, informing users they only get one guess
                CPH.SendMessage($"It's time to T-t-t-t-t-trivia! Click my name, click WHISPER to send your guess to me! You have {minutes} minute(s) and {seconds} second(s) to answer the following question.: {question}. Choices: {string.Join(", ", allAnswers)}", true);
                CPH.SendYouTubeMessage($"It's time to T-t-t-t-t-trivia! Click my name, click WHISPER to send your guess to me! You have {minutes} minute(s) and {seconds} second(s) to answer the following question.: {question}. Choices: {string.Join(", ", allAnswers)}");

                // Start the timer for the duration specified in the triviaTimer global variable
                timer = new Timer(triviaTimer);
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = true;
                timer.Enabled = true;
            }
            else
            {
                // Output a message to the chat if there was an error retrieving the trivia data
                CPH.SendMessage("Failed to retrieve trivia data.", true);
                CPH.SendYouTubeMessage("Failed to retrieve trivia data.");
            }
        }
        catch (Exception ex)
        {
            // Output the exception to the chat
            CPH.SendMessage($"An error occurred: {ex.Message}", true);
            CPH.SendYouTubeMessage($"An error occurred: {ex.Message}");
        }

        return true;
    }

    private void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        // End the game if the timer runs out
        timerCount++;
        if (timerCount == 1)
        {
            double triviaTimer = CPH.GetGlobalVar<double>("triviaTimer");
            int triviaTimerSeconds = (int)(triviaTimer / 1000);
            int remainingSeconds = triviaTimerSeconds / 2;

            CPH.SendMessage($"Reminder: You have {remainingSeconds} seconds left to answer the trivia question!", true);
            CPH.SendYouTubeMessage($"Reminder: You have {remainingSeconds} seconds left to answer the trivia question!");
            return;
        }
        else
        {
            CPH.SendMessage("Time's up! The correct answer was: " + correctAnswer, true);
            CPH.SendYouTubeMessage("Time's up! The correct answer was: " + correctAnswer);
            timer.Enabled = false;
        }
    }

public bool TriviaAnswerHandler()
{
    string user = args["user"].ToString();
    string rawInput = args["rawInput"].ToString();

    string guess;

    // Check if the message starts with !guess
    if (rawInput.StartsWith("!guess"))
    {
        // If it does, it's from YouTube, so extract the guess from the message
        guess = rawInput.Substring("!guess".Length).Trim();
    }
    else
    {
        // If it doesn't, it's a whisper from Twitch, so the entire message is the guess
        guess = rawInput;
    }

    // Check if the user already guessed
    if (guessedUsers.Contains(user))
    {
        // Notify the user that they've already guessed and return
        CPH.SendMessage($"{user}, you've already guessed! Please wait for the next question.", true);
        CPH.SendYouTubeMessage($"{user}, you've already guessed! Please wait for the next question.");
        return true;
    }

    // Add the user to the set of guessed users
    guessedUsers.Add(user);

    // Check if the answer is correct
    if (guess.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
    {
        // Increment user's points
        int points = GetUserPoints(user) + 1;
        CPH.SetTwitchUserVar(user, "TriviaPoints", points);
        CPH.SetYouTubeUserVar(user, "TriviaPoints", points);

        // End the game and announce the winner
        CPH.SendMessage($"Congratulations, {user}! You answered correctly with {correctAnswer} and earned a point. You now have a total of {points} trivia points!", true);
        CPH.SendYouTubeMessage($"Congratulations, {user}! You answered correctly with {correctAnswer} and earned a point. You now have a total of {points} trivia points!");
        timer.Stop();
        reminderTimer.Stop();
    }
    else
    {
        // Notify the user of incorrect guess
        CPH.SendMessage($"Sorry, {user}, that's incorrect. You're out of guesses!", true);
        CPH.SendYouTubeMessage($"Sorry, {user}, that's incorrect. You're out of guesses!");
    }

    return true;
}


    private int GetUserPoints(string user)
    {
        // Get user's points from persistent variable
        if (CPH.GetTwitchUserVar<int>(user, "TriviaPoints") != null)
        {
            return CPH.GetTwitchUserVar<int>(user, "TriviaPoints");
        }
        else
        {
            return 0;
        }
    }

    public bool TriviaPoints()
    {
        string user = args["user"].ToString();
        int points = GetUserPoints(user);
        CPH.SendMessage($"{user}, you have {points} trivia points.", true);
        CPH.SendYouTubeMessage($"{user}, you have {points} trivia points.");
        return true;
    }

    public bool TriviaLeaderboard()
    {
        // Get a list of all Twitch users with the "TriviaPoints" variable, along with their values
        var leaderboard = CPH.GetTwitchUsersVar<int>("TriviaPoints");
        // Sort the leaderboard in descending order by points
        leaderboard.Sort((user1, user2) => user2.Value.CompareTo(user1.Value));
        // Create a message with the leaderboard
        string leaderboardMessage = "Trivia Points Leaderboard:\n";
        for (int i = 0; i < Math.Min(10, leaderboard.Count); i++)
        {
            string userName = leaderboard[i].UserName;
            int points = leaderboard[i].Value;
            leaderboardMessage += $"{i + 1}. {userName}: {points} points\n";
        }

        // Send the leaderboard to the chat
        CPH.SendMessage(leaderboardMessage, true);
        CPH.SendYouTubeMessage(leaderboardMessage);
        return true;
    }
}
