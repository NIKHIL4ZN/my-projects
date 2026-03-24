using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Media;
using System.Collections.Generic;
using System.Linq;

namespace ChatbotWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Declared private variables
        private Dictionary<string, string[]> chatbotResponses;
        private List<(string task, DateTime reminderDate)> reminders = new List<(string, DateTime)>();
        private List<string> activityLog = new List<string>();
        private bool isNameEntered = false;
        private string userName = "";
        private string lastMatchedTopic = null; // Stores last detected keyword topic

        public MainWindow()
        {
            InitializeComponent();


        }

        // Chatbot styling for responses
        private async Task AddStyledChatbotMessage(string message, Color textColor, Color backgroundColor)
        {
            Border messageContainer = new Border
            {
                Background = new SolidColorBrush(backgroundColor),  // Sets background color properly
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5),
                Margin = new Thickness(5)
            };

            TextBlock styledMessage = new TextBlock
            {
                Text = "",
                Foreground = new SolidColorBrush(textColor),  // Sets text color
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };

            messageContainer.Child = styledMessage; // Sets textBlock inside border
            ChatHistoryListBox.Items.Add(messageContainer); // Add to chat history

            // Apply typing effect dynamically
            foreach (char letter in message)
            {
                styledMessage.Text += letter;
                await Task.Delay(30); // Adjust typing speed
            }
        }


        // Typing effect
        private async Task TypeEffect(string message, TextBlock textBlock)
        {
            textBlock.Text = ""; // Clear text

            foreach (char letter in message)
            {
                textBlock.Text += letter;
                await Task.Delay(65); 
            }
        }

        // Sound player used to play greeting
        SoundPlayer Greeting = new SoundPlayer("Greeting.wav");

        // Greeting message
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Plays greeting audio
            Greeting.Play();

            // Display greeting in the separate ChatbotGreetingText at startup
            await TypeEffect("Hello! Welcome to the Cybersecurity Awareness Bot. I'm here to help you stay safe online.", ChatbotGreetingText);

            // Add name prompt to chat history
            await Task.Delay(1000);
            await AddStyledChatbotMessage("ChatBot: Could you kindly share your name with me?", Colors.DarkMagenta, Colors.White);

            // Adds activity message to activity log
            if (activityLog.Count > 0)
            {
                string lastAction = activityLog.Last();
                await AddStyledChatbotMessage($"📜 Last Recorded Action: {lastAction}", Colors.Blue, Colors.White);
            }

            // Displays reminder message and removes reminder message 
            foreach (var reminder in reminders.ToList()) 
            {
                if (reminder.reminderDate.Date == DateTime.Now.Date)
                {
                    await AddStyledChatbotMessage($"⏰ Reminder Due Today: {reminder.task}", Colors.Red, Colors.White);
                    reminders.Remove(reminder); // Removes reminders after notifying user
                }
            }
            }

        // Button to delete score history
        private void ClearScoresButton_Click(object sender, RoutedEventArgs e)
        {
            pastScores.Clear();
            ScoresListBox.Items.Clear();
        }

        // Button to delete reminders history
        private void ClearRemindersButton_Click(object sender, RoutedEventArgs e)
        {
            reminders.Clear();
            RemindersListBox.Items.Clear();
        }

        // Button to delete activity log history
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            activityLog.Clear();
            ActivityLogListBox.Items.Clear();
        }

        // Variables declared below
        private bool isQuizActive = false;
        private int currentQuestionIndex = 0;
        private int score = 0;

        private List<string> pastScores = new List<string>();
        private DateTime quizStartTime;

        // Saves quiz game score information in mini-game quiz score history tab
        private async Task SaveQuizScore()
        {
            string scoreEntry = $"Quiz {pastScores.Count + 1}: {score}/10 | Started at {quizStartTime:HH:mm:ss dd-MM-yyyy}";
            pastScores.Add(scoreEntry);

            ScoresListBox.Items.Add(scoreEntry);
        }

        // List that stores quiz questions
        private readonly List<(string question, string[] options, object correctAnswer)> quizQuestions = new List<(string, string[], object)>
{  
    ("Plese enter in a number or either 'True'/'False'\nWhat does HTTP stand for?", new string[] { "1. HyperText Transfer Protocol", "2. High Transfer Technology Protocol", "3. HyperText Transmission Protocol", "4. High Transmission Technology Protocol" }, 0),
    ("Which of the following is a strong password?", new string[] { "1. 123456", "2. password", "3. Qw3rty!@#", "4. abcdef" }, 2),
    ("What does VPN stand for?", new string[] { "1. Virtual Private Network", "2. Verified Protocol Network", "3. Virtual Protected Network", "4. Verified Private Network" }, 0),
    ("Which of the following is an example of malware?", new string[] { "1. Firewall", "2. Antivirus", "3. Trojan Horse", "4. VPN" }, 2),
    ("What is the main purpose of encryption?", new string[] { "1. To speed up data transmission", "2. To protect data", "3. To delete sensitive information", "4. To hack into systems" }, 1),
    ("What is a firewall used for?", new string[] { "1. To hack into systems", "2. To protect a network", "3. To encrypt data", "4. To store passwords" }, 1),  
    ("A firewall is used to hack into systems.", new string[] { "True", "False" }, false),
    ("Phishing is a method to steal sensitive information by pretending to be a trustworthy entity.", new string[] { "True", "False" }, true),
    ("Using 'password' as your password makes your account more secure.", new string[] { "True", "False" }, false),
    ("Public Wi-Fi networks are always safe to use without protection.", new string[] { "True", "False" }, false),
};

        // Handles all chatbots conversation 
        private async Task HandleChatbotConversation()
        {
            Dictionary<string, string[]> chatbotResponses = this.chatbotResponses; // Reference global dictionary
            Random random = new Random();

            // Words that the user might ask in case of confusion
            string[] confusionKeywords = {
                    "i don't understand",
                    "can you explain",
                    "explain",
                    "more info",
                    "more",
                    "details",
                    "what do you mean",
                    "confusing",
                    "confused",
                    "elaborate",
                    "further"
            };

            // Clear users input after user inputs a message
            string userInput = ChatInputTextBox.Text.Trim().ToLower();

            // Validation for null input from user
            if (string.IsNullOrWhiteSpace(userInput))
            {
                await AddStyledChatbotMessage("ChatBot: I didn’t catch that. Could you enter something?", Colors.Red, Colors.White);
                return;
            }


            // Handling user confusion message requests
            if (confusionKeywords.Any(phrase => userInput.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
            {
                // Gets last matched topic from the dictionery and select another response relating to the same topic
                if (!string.IsNullOrEmpty(lastMatchedTopic) && chatbotResponses.ContainsKey(lastMatchedTopic))
                {
                    string[] responses = chatbotResponses[lastMatchedTopic];
                    string selectedResponse = responses[random.Next(responses.Length)];
                    await AddStyledChatbotMessage($"ChatBot: Let me clarify that further — {selectedResponse}", Colors.DarkMagenta, Colors.White);
                    return;
                }
                else
                {
                    // Displays error message
                    await AddStyledChatbotMessage("ChatBot: I'm not sure what to expand on. Could you ask your question again?", Colors.Red, Colors.White);
                    return;
                }
            }


            // Start quiz when user types "start quiz"
            if (userInput == "start quiz")
            {
                isQuizActive = true;
                currentQuestionIndex = 0;
                score = 0;
                quizStartTime = DateTime.Now;

                string logEntry = $"[🎯 Quiz Started] ({DateTime.Now:HH:mm:ss dd-MM-yyyy})";
                activityLog.Add(logEntry);
                ActivityLogListBox.Items.Add(logEntry);

                await AskQuizQuestion();
                return;
            }

            // Stop quiz when user types "stop quiz"
            if (isQuizActive && userInput == "stop quiz")
            {
                isQuizActive = false;
                await AddStyledChatbotMessage($"ChatBot: Quiz stopped. Your final score is {score}/{quizQuestions.Count}! How can I assist you today?", Colors.DarkMagenta, Colors.White);
                return;
            }

            // Process quiz answers when quiz is active
            if (isQuizActive)
            {
                await ValidateQuizAnswer(userInput);
                return;
            }

            // Sets a reminder when user types "Remind me to"
            if (userInput.StartsWith("remind me to", StringComparison.OrdinalIgnoreCase))
            {
                // Spliys the user input into 2 parts to separate reminder description and reminder date
                string[] parts = userInput.Split(" in ");

                // If incorrect reminder format it entered
                if (parts.Length < 2 || !int.TryParse(parts[1].Replace(" days", "").Trim(), out int days))
                {
                    await AddStyledChatbotMessage("ChatBot: Please use this format — 'Remind me to [task] in [X] days'.", Colors.Red, Colors.White);
                    return;
                }

                // Modify user reminder into a reminder message that can be stored with date format
                string task = parts[0].Replace("remind me to", "").Trim();
                DateTime reminderDate = DateTime.Now.AddDays(days);

                reminders.Add((task, reminderDate));
                RemindersListBox.Items.Add($"🔔 {task} - Due on {reminderDate:dd-MM-yyyy}");

                string logEntry = $"[📌 Reminder Set] '{task}' in {days} days ({DateTime.Now:HH:mm:ss dd-MM-yyyy})";
                activityLog.Add(logEntry);
                ActivityLogListBox.Items.Add(logEntry);

                await AddStyledChatbotMessage($"ChatBot: Reminder set! I’ll remind you about \"{task}\" in {days} days.", Colors.Green, Colors.White);
                return;
            }


            // Match user input with responses
            foreach (var key in chatbotResponses.Keys.OrderByDescending(k => k.Length))
            {
                if (userInput.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    string[] possibleResponses = chatbotResponses[key];

                    // Replace username dynamically so responses related to username is possible
                    string selectedResponse = possibleResponses[random.Next(possibleResponses.Length)]
                        .Replace("{userName}", userName);

                    await AddStyledChatbotMessage($"ChatBot: {selectedResponse}", Colors.DarkMagenta, Colors.White);
                    lastMatchedTopic = key; // Store last detected topic
                    return;
                }
            }

            // If response is not found
            await AddStyledChatbotMessage("ChatBot: Sorry, I don’t have an answer for that.", Colors.Red, Colors.White);
        }

        // Handles quiz questions
        private async Task AskQuizQuestion()
        {
            if (currentQuestionIndex >= quizQuestions.Count)
            {
                isQuizActive = false;
                await SaveQuizScore();
                await AddStyledChatbotMessage($"ChatBot: Quiz complete! Your final score is {score}/{quizQuestions.Count}.", Colors.DarkMagenta, Colors.White);

                // Provide feedback based on score performance
                if (score >= 8)
                {
                    await AddStyledChatbotMessage("🌟 Amazing! You're a cybersecurity expert! 🏆", Colors.Green, Colors.White);
                }
                else if (score >= 5)
                {
                    await AddStyledChatbotMessage("✅ Great job! Keep learning and improving!", Colors.Blue, Colors.White);
                }
                else
                {
                    await AddStyledChatbotMessage("⚠ Stay sharp! Cybersecurity knowledge is key to online safety.", Colors.Red, Colors.White);
                }

                await AddStyledChatbotMessage("ChatBot: How can I assist you today?", Colors.DarkMagenta, Colors.White);
                return;
            }

            // Ask the next question
            var questionData = quizQuestions[currentQuestionIndex];
            await AddStyledChatbotMessage($"ChatBot: {questionData.question}\n{string.Join("\n", questionData.options)}", Colors.DarkMagenta, Colors.White);
        }

        // Marks quiz questions
        private async Task ValidateQuizAnswer(string userInput)
        {
            var questionData = quizQuestions[currentQuestionIndex];

            // Check multiple-choice questions 
            if (questionData.correctAnswer is int correctIndex)
            {
                if (int.TryParse(userInput, out int userAnswer) && userAnswer - 1 == correctIndex)
                {
                    score++;
                    await AddStyledChatbotMessage($"ChatBot: ✅ Correct! Your score is {score}/{quizQuestions.Count}.", Colors.DarkMagenta, Colors.White);
                }
                else
                {
                    await AddStyledChatbotMessage($"ChatBot: ❌ Incorrect. The correct answer was {questionData.options[correctIndex]}. Your score is {score}/{quizQuestions.Count}.", Colors.DarkMagenta, Colors.White);
                }
            }
            // Marks true/false questions 
            else if (questionData.correctAnswer is bool correctBool)
            {
                if ((userInput == "true" && correctBool) || (userInput == "false" && !correctBool))
                {
                    score++;
                    await AddStyledChatbotMessage($"ChatBot: ✅ Correct! Your score is {score}/{quizQuestions.Count}.", Colors.DarkMagenta, Colors.White);
                }
                else
                {
                    await AddStyledChatbotMessage($"ChatBot: ❌ Incorrect. The correct answer was {(correctBool ? "True" : "False")}. Your score is {score}/{quizQuestions.Count}.", Colors.DarkMagenta, Colors.White);
                }
            }

            // Adds to question index so that question counter works
            currentQuestionIndex++;
            await AskQuizQuestion();
        }

        // Handles users first input
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = ChatInputTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(userInput))
            {
                // Checks if username has been entered
                if (!isNameEntered)
                {
                    await AddStyledChatbotMessage("ChatBot: I need a name to personalize our chat. Please enter your name.", Colors.Red, Colors.White);
                }
                else
                {
                    await AddStyledChatbotMessage("ChatBot: I didn’t catch that. Please enter something.", Colors.Red, Colors.White);
                }
                return;
            }

            await AddStyledChatbotMessage($"You: {userInput}", Colors.Black, Colors.White);

            if (!isNameEntered)
            {
                userName = userInput;
                await AddStyledChatbotMessage($"ChatBot: Nice to meet you, {userName}!", Colors.DarkMagenta, Colors.White);
                await AddStyledChatbotMessage("ChatBot: How can I assist you today?", Colors.DarkMagenta, Colors.White);
                isNameEntered = true;
                ChatInputTextBox.Clear(); // Reset input field

                // Dictionary stores all chatbot triggers and responses  
                chatbotResponses = new Dictionary<string, string[]>
                {
                       {"hello", new string[]
    {
        $"Hi there {userName}!",
        $"Hello {userName}, nice to meet you!",
        $"Hey there {userName}! How can I assist you today?"
    }
},

                          {"hey", new string[]
    {
        $"Hi there {userName}!",
        $"Hello {userName}, nice to meet you!",
        $"Hey there {userName}! How can I assist you today?"
    }
},

                           {"hi", new string[]
    {
        $"Hi there {userName}!",
        $"Hello {userName}, nice to meet you!",
        $"Hey there {userName}! How can I assist you today?"
    }
},

{ "feeling", new string[]
    {
        $"Thanks for asking {userName}! I don’t have emotions like humans do, but if I did, I’d say I’m feeling great helping you stay cyber-secure!",
        $"I’m doing well, thanks {userName}! I exist to make sure you stay safe online.",
        $"No feelings here, just pure cybersecurity knowledge! Ready to assist {userName}."
    }
},

{ "name", new string[]
    {
        $"I'm your chatbot assistant, designed to keep you cyber-secure! I may not have a fancy name like {userName}, but I do have plenty of knowledge on online safety!",
        "You can call me your cybersecurity guide! My mission? Keeping you safe online.",
        "Name? I don’t have one, but I do have a wealth of cybersecurity knowledge to share!"
    }
},

{ "purpose", new string[]
    {
    "My purpose is to help you stay safe online by providing cybersecurity tips and guidance. Ask me about phishing, password security, safe browsing, and more!",
    "I exist to educate and assist users in staying secure online. Whether it's password safety, phishing prevention, or browsing securely, I'm here to help!",
    "I'm designed to improve your cybersecurity awareness! Ask me anything about online security, and I'll do my best to protect you from digital threats.",
    }
},

 { "what can you do", new string[]
    {
    "My purpose is to help you stay safe online by providing cybersecurity tips and guidance. Ask me about phishing, password security, safe browsing, and more!",
    "I exist to educate and assist users in staying secure online. Whether it's password safety, phishing prevention, or browsing securely, I'm here to help!",
    "I'm designed to improve your cybersecurity awareness! Ask me anything about online security, and I'll do my best to protect you from digital threats.",
    }
},

{ "bye", new string[]
    {
    $"Goodbye {userName}! Stay safe and secure online.",
    $"Take care {userName}! Stay cyber-aware!",
    $"Bye for now {userName}! Remember, cybersecurity is key!"
    }
},

{ "worried", new string[]
    {
    "I hear you. Cyber threats can feel overwhelming, but you're not alone. It's okay to be cautious—staying informed is a powerful step. Ask me more questions if you are unsure of anything",
    "That’s completely understandable. The online world has risks, but being mindful already puts you ahead of scammers and hackers. Ask me more questions if you are unsure of anything",
    "I get that—it can be unsettling knowing there are cyber threats out there. Just remember, you have the awareness and ability to stay safe. Ask me more questions if you are unsure of anything",
    "You're right to be concerned. The digital world has its dangers, but you're capable of navigating it wisely. I'm here whenever you need reassurance. Ask me more questions if you are unsure of anything",
    "I understand your worry. Cybersecurity is constantly evolving, and while risks exist, you're doing the right thing by staying alert and informed. Ask me more questions if you are unsure of anything"
    }
},

{ "scared", new string[]
    {
    "I hear you. Cyber threats can feel overwhelming, but you're not alone. It's okay to be cautious—staying informed is a powerful step. Ask me more questions if you are unsure of anything",
    "That’s completely understandable. The online world has risks, but being mindful already puts you ahead of scammers and hackers. Ask me more questions if you are unsure of anything",
    "I get that—it can be unsettling knowing there are cyber threats out there. Just remember, you have the awareness and ability to stay safe. Ask me more questions if you are unsure of anything",
    "You're right to be concerned. The digital world has its dangers, but you're capable of navigating it wisely. I'm here whenever you need reassurance. Ask me more questions if you are unsure of anything",
    "I understand your worry. Cybersecurity is constantly evolving, and while risks exist, you're doing the right thing by staying alert and informed. Ask me more questions if you are unsure of anything"
    }
},

{ "frustrated", new string[]
    {
    "I hear you. Cyber threats can feel overwhelming, but you're not alone. It's okay to be cautious—staying informed is a powerful step. Ask me more questions if you are unsure of anything",
    "That’s completely understandable. The online world has risks, but being mindful already puts you ahead of scammers and hackers. Ask me more questions if you are unsure of anything",
    "I get that—it can be unsettling knowing there are cyber threats out there. Just remember, you have the awareness and ability to stay safe. Ask me more questions if you are unsure of anything",
    "You're right to be concerned. The digital world has its dangers, but you're capable of navigating it wisely. I'm here whenever you need reassurance. Ask me more questions if you are unsure of anything",
    "I understand your worry. Cybersecurity is constantly evolving, and while risks exist, you're doing the right thing by staying alert and informed. Ask me more questions if you are unsure of anything"
    }
},

{ "curious", new string[]
    {
    "I hear you. Cyber threats can feel overwhelming, but you're not alone. It's okay to be cautious—staying informed is a powerful step. Ask me more questions if you are unsure of anything",
    "That’s completely understandable. The online world has risks, but being mindful already puts you ahead of scammers and hackers. Ask me more questions if you are unsure of anything",
    "I get that—it can be unsettling knowing there are cyber threats out there. Just remember, you have the awareness and ability to stay safe. Ask me more questions if you are unsure of anything",
    "You're right to be concerned. The digital world has its dangers, but you're capable of navigating it wisely. I'm here whenever you need reassurance. Ask me more questions if you are unsure of anything",
    "I understand your worry. Cybersecurity is constantly evolving, and while risks exist, you're doing the right thing by staying alert and informed. Ask me more questions if you are unsure of anything"
    }
},

{ "Thank you", new string[]
   {
   $"You're very welcome {userName}! I'm always here to help.",
   "No problem at all! Glad I could assist.",
   $"You're welcome {userName}! Stay safe online.",
   $"Happy to help you {userName}! Let me know if you need anything else."
   }
},

{ "Thanks", new string[]
   {
   $"You're very welcome {userName}! I'm always here to help.",
   "No problem at all! Glad I could assist.",
   $"You're welcome {userName}! Stay safe online.",
   $"Happy to help you {userName}! Let me know if you need anything else."
   }
},


{ "actors", new string[]
    {
      "Cybersecurity threat actors are individuals or groups that carry out cyberattacks. These include cybercriminals (financially motivated), nation-state actors (espionage and warfare), hacktivists (ideological attacks), insiders (employees misusing access), script kiddies (amateur hackers), cyber terrorists (disruptive attacks), and APTs (long-term espionage). Understanding them helps strengthen security defenses. Which of these would you want to know more about?"
    }
},


{ "threats", new string[]
    {
      "Cybersecurity threat actors are individuals or groups that carry out cyberattacks. These include cybercriminals (financially motivated), nation-state actors (espionage and warfare), hacktivists (ideological attacks), insiders (employees misusing access), script kiddies (amateur hackers), cyber terrorists (disruptive attacks), and APTs (long-term espionage). Understanding them helps strengthen security defenses. Which of these would you want to know more about?"
    }
},

{ "threat", new string[]
    {
      "Cybersecurity threat actors are individuals or groups that carry out cyberattacks. These include cybercriminals (financially motivated), nation-state actors (espionage and warfare), hacktivists (ideological attacks), insiders (employees misusing access), script kiddies (amateur hackers), cyber terrorists (disruptive attacks), and APTs (long-term espionage). Understanding them helps strengthen security defenses. Which of these would you want to know more about?"
    }
},

{ "hackers", new string[]
    {
      "Cybersecurity threat actors are individuals or groups that carry out cyberattacks. These include cybercriminals (financially motivated), nation-state actors (espionage and warfare), hacktivists (ideological attacks), insiders (employees misusing access), script kiddies (amateur hackers), cyber terrorists (disruptive attacks), and APTs (long-term espionage). Understanding them helps strengthen security defenses. Which of these would you want to know more about?"
    }
},

{ "hacker", new string[]
    {
      "Cybersecurity threat actors are individuals or groups that carry out cyberattacks. These include cybercriminals (financially motivated), nation-state actors (espionage and warfare), hacktivists (ideological attacks), insiders (employees misusing access), script kiddies (amateur hackers), cyber terrorists (disruptive attacks), and APTs (long-term espionage). Understanding them helps strengthen security defenses. Which of these would you want to know more about?"
    }
},

{ "cybercriminals", new string[]
     {
      "Cybercriminals are individuals or groups who engage in illegal activities online, such as hacking, fraud, and ransomware attacks, primarily for financial gain.",
      "Cybercriminals are people who break into systems or steal data online to make money."
     }
},

{ "cybercriminal", new string[]
     {
      "Cybercriminals are individuals or groups who engage in illegal activities online, such as hacking, fraud, and ransomware attacks, primarily for financial gain.",
      "Cybercriminals are people who break into systems or steal data online to make money."
     }
},

{  "nation", new string[]
     {
     "Nation-state actors are government-backed hackers who conduct cyber espionage, sabotage, or cyber warfare to advance their country's interests.",
     "Nation-state actors are hackers working for a government to spy on or attack other countries."
     }
},


{ "hacktivists", new string[]
     {
    "Hacktivists are individuals or groups who use hacking techniques to promote political, social, or ideological causes, often by disrupting or exposing organizations.",
    "Hacktivists are hackers who attack systems to support a cause or protest something."
     }
},

{ "hacktivist", new string[]
     {
    "Hacktivists are individuals or groups who use hacking techniques to promote political, social, or ideological causes, often by disrupting or exposing organizations.",
    "Hacktivists are hackers who attack systems to support a cause or protest something."
     }
},

{ "insiders", new string[]
     {
   "Insiders are employees, contractors, or partners who misuse their access to steal data, sabotage systems, or leak confidential information—either intentionally or accidentally.",
   "Insiders are people inside a company who misuse their access to harm the organization."
     }
},

{ "insider", new string[]
     {
   "Insiders are employees, contractors, or partners who misuse their access to steal data, sabotage systems, or leak confidential information—either intentionally or accidentally.",
   "Insiders are people inside a company who misuse their access to harm the organization."
     }
},

{ "script kiddies", new string[]
     {
   "Script kiddies are inexperienced individuals who use pre-made hacking tools without deep technical knowledge, often for fun or to cause disruption.",
   "Script kiddies are beginners who use hacking tools without fully understanding them."
     }
},

{ "cyber terrorists", new string[]
     {
   "Cyber terrorists use cyberattacks to cause fear, disruption, or harm, often targeting critical infrastructure, financial systems, or government networks.",
   "Cyber terrorists are hackers who attack systems to create chaos or fear."
     }
},

{ "cyber terrorist", new string[]
     {
   "Cyber terrorists use cyberattacks to cause fear, disruption, or harm, often targeting critical infrastructure, financial systems, or government networks.",
   "Cyber terrorists are hackers who attack systems to create chaos or fear."
     }
},

{ "advanced persistent threats", new string[]
     {
   "Advanced Persistent Threats (APTs) are highly skilled, well-funded groups that conduct long-term cyber espionage, infiltrating networks and remaining undetected for extended periods.",
   "APTs are expert hackers who secretly spy on systems for a long time."
     }
},

 { "apts", new string[]
     {
     "Advanced Persistent Threats (APTs) are highly skilled, well-funded groups that conduct long-term cyber espionage, infiltrating networks and remaining undetected for extended periods.",
     "APTs are expert hackers who secretly spy on systems for a long time."
     }
},

 { "avoid", new string[]
    {
      "To avoid threat actors online, always use strong passwords, enable multi-factor authentication, and be cautious of phishing attempts. Keep your software updated, avoid suspicious links, and use a VPN for extra security. If you still feel unsure, feel free to ask me questions about phishing, password safety and safe browsing.",
      "Stay safe online by using strong passwords, avoiding suspicious links, and keeping your software updated. Threat actors exploit weak security, so staying alert is key! If you still feel unsure, feel free to ask me questions about phishing, password safety and safe browsing."
    }
 },

  { "clear", new string[]
    {
      "To avoid threat actors online, always use strong passwords, enable multi-factor authentication, and be cautious of phishing attempts. Keep your software updated, avoid suspicious links, and use a VPN for extra security. If you still feel unsure, feel free to ask me questions about phishing, password safety and safe browsing.",
      "Stay safe online by using strong passwords, avoiding suspicious links, and keeping your software updated. Threat actors exploit weak security, so staying alert is key! If you still feel unsure, feel free to ask me questions about phishing, password safety and safe browsing."
    }
 },


{ "password", new string[]
    {
        "Your password is your first line of defense against cyber threats. A strong password should be long, unique, and include a mix of letters, numbers, and symbols. Avoid using personal information or common words, and enable multi-factor authentication for extra security.",
        "A password is like a secret key that only you should know. It helps protect your accounts from others getting in. A strong password should be long, unique, and include a mix of letters, numbers, and symbols.",
        "Just like a key keeps strangers out of your home, a strong password keeps hackers out of your online accounts. A strong password should be long, unique, and include a mix of letters, numbers, and symbols.",
    }
},

{ "password tips", new string[]
    {
        "Use a password that is at least 16 characters long and includes uppercase letters, lowercase letters, numbers, and symbols.",
        "Avoid using personal information like your name, birthdate, or pet’s name in passwords—these can be easily guessed.",
        "Never reuse passwords across multiple accounts—if one gets compromised, all linked accounts could be at risk.",
        "Enable multi-factor authentication (MFA) whenever possible to add an extra layer of security.",
        "Use a password manager to generate and store strong passwords securely.",
        "Consider using passphrases instead of traditional passwords—random words strung together can be easier to remember and harder to crack.",
        "Change your passwords periodically, especially for sensitive accounts like banking and email.",
        "Be cautious of password reset emails—always verify the sender before clicking any links.",
        "Avoid saving passwords in browsers—use a dedicated password manager instead.",
        "If a website offers security questions, choose answers that are difficult to guess and unrelated to your personal life."
    }
},

{ "phishing", new string[]
    {
        "Phishing is a cyber attack where hackers impersonate trusted entities to steal personal information. Always double-check sender email addresses before clicking links.",
        "Phishing is when someone pretends to be a trusted person or company to trick you into giving away personal information.",
        "Phishing emails or messages often look like they’re from real companies, but they’re fakes trying to steal from you.",
    }
},

{ "phishing tips",  new string[]
    {
        "Never click on suspicious links in emails or messages! Phishing scams often create urgency to trick you.",
        "Always verify the sender's email address before clicking any links—phishers often mimic trusted companies.",
        "Look for spelling errors or unusual formatting in emails—these are common signs of phishing attempts.",
        "Enable multi-factor authentication (MFA) on all accounts to add an extra layer of security.",
        "Never enter personal information on websites unless you're sure they're legitimate. Check for HTTPS in the URL!",
        "If an email or message asks for sensitive information, contact the company directly instead of clicking any links.",
        "Use a password manager to generate and store strong passwords—this helps prevent credential theft from phishing attacks.",
        "Be cautious of emails claiming you've won a prize or need to reset your password urgently—these are common phishing tactics.",
    }
},

{ "browsing", new string[]
    {
        "Safe browsing refers to practices that protect users from online threats, such as phishing, malware, and data theft. It involves using secure websites, avoiding suspicious links, keeping software updated, and enabling security features to stay safe while navigating the internet.",
        "Safe browsing means using the internet carefully—like checking if a website is secure and not clicking on strange links.",
        "Before clicking on a link or downloading something from the internet, you check if it’s safe to avoid danger on the browser."
    }
},

{ "browsing tips", new string[]
    {
       "Always check for HTTPS in website URLs—this ensures your connection is encrypted and secure.",
       "Avoid clicking on pop-ups or suspicious ads—they often contain malware or phishing attempts.",
       "Keep your browser updated! Security patches help protect you from online threats.",
       "Use a VPN when browsing on public Wi-Fi to prevent hackers from intercepting your data.",
       "Enable ad blockers to reduce exposure to malicious ads and tracking scripts.",
       "Never download files from unknown websites—many malware infections come from suspicious downloads.",
       "Be cautious when entering personal information online—only provide details on trusted websites.",
       "Use strong, unique passwords for online accounts and enable multi-factor authentication (MFA) for extra security.",
    }
}

                };
                return;
            }

            HandleChatbotConversation(); // Continues after name is entered
            ChatInputTextBox.Clear();
            if (!isQuizActive)
            {
                await AddStyledChatbotMessage("ChatBot: How can I assist you today?", Colors.DarkMagenta, Colors.White);
            }
           
        }
    }
}
