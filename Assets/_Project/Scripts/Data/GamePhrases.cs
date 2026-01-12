using System.Collections.Generic;

namespace Cerebrum.Data
{
    public static class GamePhrases
    {
        public enum PhraseCategory
        {
            BuzzIn,
            Correct,
            AnyoneElse,
            Incorrect,
            SelectCategory,
            SelectCategoryFirst,  // For first pick of the game only
            Timeout,
            RevealAnswer,
            DailyDouble,
            FinalJeopardy,
            GameFlow
        }

        public class Phrase
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public PhraseCategory Category { get; set; }
            public bool NamePrefix { get; set; }  // Play [Name] before this phrase
            public bool NameSuffix { get; set; }  // Play [Name] after this phrase

            /// <summary>
            /// True if this phrase can be pre-generated and bundled with the app.
            /// False if it requires runtime data (player names, dynamic text).
            /// </summary>
            public bool IsBundleable => !NamePrefix && !NameSuffix;

            public Phrase(string id, string text, PhraseCategory category, bool namePrefix = false, bool nameSuffix = false)
            {
                Id = id;
                Text = text;
                Category = category;
                NamePrefix = namePrefix;
                NameSuffix = nameSuffix;
            }
        }

        public static readonly List<Phrase> AllPhrases = new List<Phrase>
        {
            // ===== BUZZ-IN ACKNOWLEDGMENT =====
            // Usage: [PlayerName] → phrase
            new Phrase("buzz_yes_1", "Yes?", PhraseCategory.BuzzIn, namePrefix: true),
            new Phrase("buzz_yes_2", "Go ahead.", PhraseCategory.BuzzIn, namePrefix: true),
            new Phrase("buzz_go", "Go.", PhraseCategory.BuzzIn, namePrefix: true),

            // ===== CORRECT ANSWER =====
            // Usage: phrase → optional [PlayerName]
            new Phrase("correct_1", "Correct!", PhraseCategory.Correct, nameSuffix: true),
            new Phrase("correct_2", "That's right!", PhraseCategory.Correct, nameSuffix: true),
            new Phrase("correct_3", "Yes!", PhraseCategory.Correct, nameSuffix: true),
            new Phrase("correct_4", "Absolutely!", PhraseCategory.Correct, nameSuffix: true),
            new Phrase("correct_5", "You got it!", PhraseCategory.Correct),
            new Phrase("correct_6", "Well done!", PhraseCategory.Correct),
            new Phrase("correct_7", "Right you are!", PhraseCategory.Correct),
            new Phrase("correct_8", "Nicely done!", PhraseCategory.Correct),
            new Phrase("correct_9", "That is correct!", PhraseCategory.Correct),
            new Phrase("correct_10", "Excellent!", PhraseCategory.Correct),

            // ===== ANYONE ELSE (after wrong answer, others can buzz) =====
            new Phrase("anyone_1", "Anyone else?", PhraseCategory.AnyoneElse),
            new Phrase("anyone_2", "Anyone?", PhraseCategory.AnyoneElse),
            new Phrase("anyone_3", "Anybody?", PhraseCategory.AnyoneElse),

            // ===== INCORRECT ANSWER =====
            // Usage: phrase (some work with name suffix)
            new Phrase("wrong_1", "No.", PhraseCategory.Incorrect),
            new Phrase("wrong_2", "Sorry, no.", PhraseCategory.Incorrect),
            new Phrase("wrong_3", "I'm afraid not.", PhraseCategory.Incorrect),
            new Phrase("wrong_4", "Ooh, no.", PhraseCategory.Incorrect),
            new Phrase("wrong_5", "Not quite.", PhraseCategory.Incorrect),
            new Phrase("wrong_6", "Incorrect.", PhraseCategory.Incorrect),
            new Phrase("wrong_7", "No, sorry.", PhraseCategory.Incorrect),
            new Phrase("wrong_8", "That's not it.", PhraseCategory.Incorrect),
            new Phrase("wrong_9", "Nope.", PhraseCategory.Incorrect),
            new Phrase("wrong_10", "Ooh, sorry.", PhraseCategory.Incorrect),

            // ===== SELECT CATEGORY PROMPT =====
            // Usage: [PlayerName] → phrase
            new Phrase("pick_1", "Your pick.", PhraseCategory.SelectCategory, namePrefix: true),
            new Phrase("pick_2", "Where to next?", PhraseCategory.SelectCategory, namePrefix: true),
            new Phrase("pick_3", "You have control of the board.", PhraseCategory.SelectCategory, namePrefix: true),
            new Phrase("pick_4", "Select a category.", PhraseCategory.SelectCategory, namePrefix: true),
            new Phrase("pick_5", "Your choice.", PhraseCategory.SelectCategory, namePrefix: true),
            new Phrase("pick_6", "Pick a clue.", PhraseCategory.SelectCategory, namePrefix: true),
            new Phrase("pick_7", "Back to you.", PhraseCategory.SelectCategory, namePrefix: true),
            new Phrase("pick_8", "You're in control.", PhraseCategory.SelectCategory, namePrefix: true),

            // ===== TIME'S UP / NO BUZZ =====
            // Standalone
            new Phrase("timeout_1", "Time's up.", PhraseCategory.Timeout),
            new Phrase("timeout_2", "Nobody?", PhraseCategory.Timeout),
            new Phrase("timeout_3", "Time.", PhraseCategory.Timeout),
            new Phrase("timeout_4", "Moving on.", PhraseCategory.Timeout),
            new Phrase("timeout_5", "Let's move on.", PhraseCategory.Timeout),

            // ===== REVEAL CORRECT ANSWER =====
            // Standalone (answer text spoken separately)
            new Phrase("reveal_1", "The correct response was", PhraseCategory.RevealAnswer),
            new Phrase("reveal_2", "We were looking for", PhraseCategory.RevealAnswer),
            new Phrase("reveal_3", "The answer is", PhraseCategory.RevealAnswer),

            // ===== DAILY DOUBLE =====
            new Phrase("dd_announce", "Daily Double!", PhraseCategory.DailyDouble),
            new Phrase("dd_wager_1", "How much would you like to wager?", PhraseCategory.DailyDouble, namePrefix: true),
            new Phrase("dd_wager_2", "What's your wager?", PhraseCategory.DailyDouble, namePrefix: true),
            new Phrase("dd_wager_3", "Name your wager.", PhraseCategory.DailyDouble, namePrefix: true),
            new Phrase("dd_allin", "A true Daily Double!", PhraseCategory.DailyDouble),

            // ===== FINAL JEOPARDY =====
            new Phrase("fj_announce", "Time for Final Jeopardy.", PhraseCategory.FinalJeopardy),
            new Phrase("fj_category", "The category is", PhraseCategory.FinalJeopardy),
            new Phrase("fj_wagers", "Players, make your wagers.", PhraseCategory.FinalJeopardy),
            new Phrase("fj_clue", "Here is your clue.", PhraseCategory.FinalJeopardy),
            new Phrase("fj_time", "You have 30 seconds. Good luck.", PhraseCategory.FinalJeopardy),
            new Phrase("fj_reveal", "Let's see what you wrote.", PhraseCategory.FinalJeopardy),
            new Phrase("fj_start_1", "Let's reveal your answers, starting with", PhraseCategory.FinalJeopardy),

            // ===== GAME FLOW =====
            new Phrase("round_end_j", "That's the end of the Jeopardy round.", PhraseCategory.GameFlow),
            new Phrase("round_start_dj", "Let's move on to Double Jeopardy.", PhraseCategory.GameFlow),
            new Phrase("round_intro_dj", "This is Double Jeopardy, where the values are doubled.", PhraseCategory.GameFlow),
            new Phrase("game_over", "That's the game!", PhraseCategory.GameFlow),
            new Phrase("thanks", "Thanks for playing.", PhraseCategory.GameFlow),
            new Phrase("good_game", "Great game, everyone.", PhraseCategory.GameFlow),
            new Phrase("lets_begin", "Let's play Jeopardy!", PhraseCategory.GameFlow),
            new Phrase("intro_welcome", "Welcome to Jeopardy.", PhraseCategory.GameFlow),
            new Phrase("todays_categories", "Today's categories are...", PhraseCategory.GameFlow),

            // ===== APP STARTUP / LOADING =====
            new Phrase("welcome_cerebrum", "Welcome to Cerebrum.", PhraseCategory.GameFlow),
            new Phrase("welcome_cerebrum_2", "Welcome to Cerebrum! The trivia game for everyone.", PhraseCategory.GameFlow),
            new Phrase("loading_moment", "Just a moment while the game loads.", PhraseCategory.GameFlow),
            new Phrase("loading_preparing", "Preparing your game.", PhraseCategory.GameFlow),
            new Phrase("loading_almost", "Almost ready.", PhraseCategory.GameFlow),
            new Phrase("loading_done", "All set! Let's play.", PhraseCategory.GameFlow),
            new Phrase("ok_start", "Okay, let's start the game.", PhraseCategory.GameFlow),
            new Phrase("ready_play", "Ready to play?", PhraseCategory.GameFlow),
            new Phrase("here_we_go", "Here we go!", PhraseCategory.GameFlow),
            new Phrase("good_luck", "Good luck, everyone!", PhraseCategory.GameFlow),
            new Phrase("good_luck_2", "Good luck to all our players.", PhraseCategory.GameFlow),

            // ===== FIRST PICK PROMPTS (for game start only) =====
            new Phrase("first_pick", "You get to pick the first category.", PhraseCategory.SelectCategoryFirst, namePrefix: true),
            new Phrase("first_pick_2", "You have first pick.", PhraseCategory.SelectCategoryFirst, namePrefix: true),
            new Phrase("start_us_off", "Start us off.", PhraseCategory.SelectCategoryFirst, namePrefix: true),
            new Phrase("pick_first", "Pick the first category.", PhraseCategory.SelectCategoryFirst, namePrefix: true),

            // ===== INSTRUCTIONS =====
            new Phrase("instr_buzz", "Buzz in when you know the answer.", PhraseCategory.GameFlow),
            new Phrase("instr_question", "Remember to phrase your response in the form of a question.", PhraseCategory.GameFlow),
            new Phrase("instr_speak", "Speak your answer clearly.", PhraseCategory.GameFlow),

            // ===== TRANSITIONS =====
            new Phrase("next_clue", "Next clue.", PhraseCategory.GameFlow),
            new Phrase("moving_on", "Moving on.", PhraseCategory.GameFlow),
            new Phrase("lets_continue", "Let's continue.", PhraseCategory.GameFlow),
            new Phrase("back_to_board", "Back to the board.", PhraseCategory.GameFlow),

            // ===== SCORES / STANDINGS =====
            new Phrase("check_scores", "Let's check the scores.", PhraseCategory.GameFlow),
            new Phrase("current_scores", "Here are the current scores.", PhraseCategory.GameFlow),
            new Phrase("close_game", "It's a close game!", PhraseCategory.GameFlow),
            new Phrase("anyone_win", "Anyone could win this.", PhraseCategory.GameFlow),

            // ===== WINNER ANNOUNCEMENTS =====
            new Phrase("winner_is", "And our winner is", PhraseCategory.GameFlow, nameSuffix: true),
            new Phrase("congratulations", "Congratulations!", PhraseCategory.GameFlow),
            new Phrase("new_champion", "We have a new champion!", PhraseCategory.GameFlow),
            new Phrase("well_played", "Well played, everyone.", PhraseCategory.GameFlow),

            // ===== EARLY BUZZ / ERRORS =====
            new Phrase("too_early", "Too early!", PhraseCategory.GameFlow),
            new Phrase("wait_for_it", "Wait for it.", PhraseCategory.GameFlow),
            new Phrase("not_yet", "Not yet!", PhraseCategory.GameFlow),
            new Phrase("hold_on", "Hold on.", PhraseCategory.GameFlow),

            // ===== PAUSE / RESUME =====
            new Phrase("game_paused", "Game paused.", PhraseCategory.GameFlow),
            new Phrase("resuming", "Resuming the game.", PhraseCategory.GameFlow),
        };

        public static List<Phrase> GetByCategory(PhraseCategory category)
        {
            return AllPhrases.FindAll(p => p.Category == category);
        }

        public static Phrase GetById(string id)
        {
            return AllPhrases.Find(p => p.Id == id);
        }

        /// <summary>
        /// Returns all phrases that can be pre-generated and bundled with the app.
        /// </summary>
        public static List<Phrase> GetBundleablePhrases()
        {
            return AllPhrases.FindAll(p => p.IsBundleable);
        }

        /// <summary>
        /// Returns phrases that require runtime generation (contain player names).
        /// </summary>
        public static List<Phrase> GetRuntimePhrases()
        {
            return AllPhrases.FindAll(p => !p.IsBundleable);
        }

        public static int TotalCount => AllPhrases.Count;
        public static int BundleableCount => AllPhrases.FindAll(p => p.IsBundleable).Count;
    }
}
