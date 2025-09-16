using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace OmnitakSupportHub.Services
{
    public class EnhancedChatbotService : ISimpleChatbotService
    {
        private readonly OmnitakContext _context;
        private readonly ILogger<EnhancedChatbotService> _logger;

  
        private static readonly Dictionary<string, ConversationContext> _conversationContexts = new();

        public EnhancedChatbotService(OmnitakContext context, ILogger<EnhancedChatbotService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> ProcessMessageAsync(string userMessage, string sessionID, int? userID = null)
        {
            try
            {
                // Get or create conversation
                var conversation = await GetConversationAsync(sessionID);
                if (conversation == null)
                {
                    conversation = await StartConversationAsync(sessionID, userID);
                }

                // Save user message
                await SaveMessageAsync(conversation.ConversationID, userMessage, "user");

                // Get conversation context
                var context = GetOrCreateContext(sessionID);
                context.MessageCount++;
                context.LastUserMessage = userMessage;

                // Detect and handle different types of input
                string response;

                if (IsGibberish(userMessage))
                {
                    response = HandleGibberish(context);
                }
                else if (IsGreeting(userMessage))
                {
                    response = HandleGreeting(context, userMessage);
                }
                else if (IsQuestion(userMessage))
                {
                    response = await HandleQuestion(userMessage, context);
                }
                else if (IsGratitude(userMessage))
                {
                    response = HandleGratitude(context);
                }
                else if (IsFarewell(userMessage))
                {
                    response = HandleFarewell(context);
                }
                else
                {
                    response = await HandleGeneralQuery(userMessage, context);
                }

                // Update context
                context.LastBotResponse = response;
                context.ConversationHistory.Add($"User: {userMessage}");
                context.ConversationHistory.Add($"Bot: {response}");

                // Keep only last 10 exchanges
                if (context.ConversationHistory.Count > 20)
                {
                    context.ConversationHistory.RemoveRange(0, 2);
                }

                // Save bot response
                await SaveMessageAsync(conversation.ConversationID, response, "bot");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                return "I apologize, but I'm experiencing some technical difficulties right now. 🤖💔 Please try again in a moment, or feel free to create a support ticket for immediate assistance!";
            }
        }

        public async Task<ChatbotConversation> StartConversationAsync(string sessionID, int? userID = null)
        {
            var conversation = new ChatbotConversation
            {
                SessionID = sessionID,
                UserID = userID,
                StartedAt = DateTime.UtcNow,
                Status = "Active"
            };

            _context.ChatbotConversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Initialize conversation context
            var context = GetOrCreateContext(sessionID);
            context.ConversationStartTime = DateTime.UtcNow;

            // Personalized welcome message
            var welcomeMessage = GetPersonalizedWelcome(userID);
            await SaveMessageAsync(conversation.ConversationID, welcomeMessage, "bot");

            return conversation;
        }

        private ConversationContext GetOrCreateContext(string sessionID)
        {
            if (!_conversationContexts.ContainsKey(sessionID))
            {
                _conversationContexts[sessionID] = new ConversationContext
                {
                    SessionId = sessionID,
                    ConversationHistory = new List<string>(),
                    ConversationStartTime = DateTime.UtcNow
                };
            }
            return _conversationContexts[sessionID];
        }

        private bool IsGibberish(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return true;

            var cleanMessage = Regex.Replace(message.ToLower(), @"[^a-z\s]", "");
            var words = cleanMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Check for random keyboard mashing
            var keyboardPatterns = new[]
            {
                "asdf", "qwerty", "zxcv", "hjkl", "mnbv", "dfgh", "jklm",
                "aaaa", "bbbb", "cccc", "dddd", "eeee", "ffff", "gggg"
            };

            foreach (var word in words)
            {
                if (word.Length > 8 && !HasVowels(word)) return true;
                if (keyboardPatterns.Any(pattern => word.Contains(pattern))) return true;
                if (word.Length > 4 && IsRepeatingPattern(word)) return true;
            }

            // Check if message has less than 30% recognizable words
            var recognizableWords = words.Where(IsLikelyRealWord).Count();
            var threshold = Math.Max(1, words.Length * 0.3);

            return recognizableWords < threshold;
        }

        private bool HasVowels(string word)
        {
            return word.Any(c => "aeiou".Contains(c));
        }

        private bool IsRepeatingPattern(string word)
        {
            for (int i = 1; i <= word.Length / 2; i++)
            {
                var pattern = word.Substring(0, i);
                var repeated = string.Concat(Enumerable.Repeat(pattern, word.Length / i));
                if (repeated == word.Substring(0, repeated.Length)) return true;
            }
            return false;
        }

        private bool IsLikelyRealWord(string word)
        {
            if (word.Length < 2) return false;

            var commonWords = new HashSet<string>
            {
                "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "man", "new", "now", "old", "see", "two", "who", "boy", "did", "its", "let", "put", "say", "she", "too", "use",
                "hello", "hi", "help", "please", "thanks", "thank", "yes", "no", "ok", "okay", "sure", "computer", "email", "password", "login", "problem", "issue", "error", "support", "ticket", "system", "network", "internet", "wifi", "printer", "software", "hardware", "windows", "microsoft", "office"
            };

            return commonWords.Contains(word) || HasVowels(word);
        }

        private bool IsGreeting(string message)
        {
            var greetings = new[] { "hello", "hi", "hey", "good morning", "good afternoon", "good evening", "greetings", "hola", "howdy" };
            return greetings.Any(g => message.ToLower().Contains(g));
        }

        private bool IsQuestion(string message)
        {
            var questionWords = new[] { "what", "how", "why", "when", "where", "who", "which", "can", "could", "would", "should", "is", "are", "do", "does", "did" };
            return message.Contains("?") || questionWords.Any(q => message.ToLower().StartsWith(q + " "));
        }

        private bool IsGratitude(string message)
        {
            var gratitudeWords = new[] { "thank", "thanks", "appreciate", "grateful", "thx", "ty" };
            return gratitudeWords.Any(g => message.ToLower().Contains(g));
        }

        private bool IsFarewell(string message)
        {
            var farewells = new[] { "bye", "goodbye", "see you", "farewell", "exit", "quit", "leave", "done", "finished" };
            return farewells.Any(f => message.ToLower().Contains(f));
        }

        private string HandleGibberish(ConversationContext context)
        {
            var responses = new[]
            {
                "I'm sorry, but I don't understand what you just said. 🤔 Could you please rephrase that?",
                "Hmm, that message seems a bit scrambled. 🔤 Could you try typing that again?",
                "I didn't quite catch that! 😅 Could you please write that in a clearer way?",
                "It looks like there might be a typing error. ⌨️ Could you try again?",
                "I'm having trouble understanding that message. 🤷‍♀️ Could you please be more specific?"
            };

            context.GibberishCount++;

            if (context.GibberishCount > 2)
            {
                return "I'm having trouble understanding your messages. 😔 Would you like me to connect you with a human support agent, or perhaps you could try describing your issue step by step?";
            }

            var random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private string HandleGreeting(ConversationContext context, string message)
        {
            var responses = new[]
            {
                "Hello there! 👋 I'm your friendly IT Support Assistant. I'm here to help you solve tech problems and answer questions!",
                "Hi! 😊 Great to see you! I'm ready to help with any IT issues or questions you might have.",
                "Hey! 🌟 Welcome to IT Support! I'm here to make your tech troubles disappear. What can I help you with?",
                "Hello! 🤖 I'm your virtual IT assistant. Whether it's passwords, emails, or any tech hiccups, I've got you covered!",
                "Hi there! ✨ I'm excited to help you today. What technology challenge can we tackle together?"
            };

            var timeOfDay = DateTime.Now.Hour < 12 ? "morning" : DateTime.Now.Hour < 17 ? "afternoon" : "evening";
            var random = new Random();
            var baseResponse = responses[random.Next(responses.Length)];

            if (message.ToLower().Contains("good " + timeOfDay))
            {
                return $"Good {timeOfDay}! " + baseResponse;
            }

            return baseResponse;
        }

        private async Task<string> HandleQuestion(string userMessage, ConversationContext context)
        {
            // First, try to search knowledge base
            var searchResults = await SearchKnowledgeBase(userMessage);

            if (searchResults.Any())
            {
                var bestMatch = searchResults.First();
                return $"Great question! 🎯 I found this in our knowledge base:\n\n**{bestMatch.Title}**\n\n{bestMatch.Summary}\n\nWould you like me to help you with anything else related to this?";
            }

            // If no knowledge base results, provide contextual help
            return await HandleGeneralQuery(userMessage, context);
        }

        private string HandleGratitude(ConversationContext context)
        {
            var responses = new[]
            {
                "You're very welcome! 😊 I'm happy I could help. Is there anything else you need assistance with?",
                "My pleasure! 🌟 That's what I'm here for. Feel free to ask if you have any other questions!",
                "Glad I could help! 💫 Don't hesitate to reach out if you run into any other issues.",
                "You're welcome! 🤗 It makes me happy when I can solve problems for you. Anything else on your mind?",
                "Anytime! ✨ I love helping solve tech puzzles. Is there anything else I can assist you with today?"
            };

            var random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private string HandleFarewell(ConversationContext context)
        {
            var responses = new[]
            {
                "Goodbye! 👋 Feel free to come back anytime you need help. Have a wonderful day!",
                "See you later! 🌟 Remember, I'm always here when you need IT support. Take care!",
                "Farewell! 💫 It was great helping you today. Don't be a stranger if you need more assistance!",
                "Bye for now! 🤖 I'll be here whenever you need tech support. Have an amazing day!",
                "Take care! ✨ Thanks for chatting with me. I'm always ready to help with your IT needs!"
            };

            var random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private async Task<string> HandleGeneralQuery(string userMessage, ConversationContext context)
        {
            var message = userMessage.ToLower();

            // Password related
            if (message.Contains("password") || message.Contains("login") || message.Contains("sign in"))
            {
                var kbResults = await SearchKnowledgeBase("password reset login");
                if (kbResults.Any())
                {
                    return $"🔐 Password troubles? I can help! Here's what I found:\n\n**{kbResults.First().Title}**\n{kbResults.First().Summary}\n\nWould you like me to create a support ticket for immediate password reset assistance?";
                }
                return "🔐 For password issues, I can help you! Our IT team can reset your password quickly. Would you like me to create a support ticket for you? Or you can try our self-service password reset portal.";
            }

            // Email related
            if (message.Contains("email") || message.Contains("outlook") || message.Contains("mail"))
            {
                var kbResults = await SearchKnowledgeBase("email outlook mail");
                if (kbResults.Any())
                {
                    return $"📧 Email problems? Let me help! I found this:\n\n**{kbResults.First().Title}**\n{kbResults.First().Summary}\n\nNeed more specific help? I can create a support ticket for you!";
                }
                return "📧 Email issues can be frustrating! I'm here to help. Common solutions include checking your internet connection, restarting Outlook, or clearing your cache. Would you like me to search for more specific solutions or create a support ticket?";
            }

            // Network/Internet related
            if (message.Contains("internet") || message.Contains("network") || message.Contains("wifi") || message.Contains("connection"))
            {
                return "🌐 Network connectivity issues? Let's troubleshoot! First, try:\n\n1️⃣ Check if your WiFi is connected\n2️⃣ Restart your router\n3️⃣ Try a different network\n\nIf these don't work, I can create a support ticket for our network specialists!";
            }

            // Printer related
            if (message.Contains("printer") || message.Contains("print") || message.Contains("printing"))
            {
                return "🖨️ Printer giving you trouble? Here are some quick fixes:\n\n1️⃣ Check if the printer is turned on and connected\n2️⃣ Restart both printer and computer\n3️⃣ Check for paper jams or low ink\n\nStill not working? I can help you create a support ticket!";
            }

            // Software related
            if (message.Contains("software") || message.Contains("application") || message.Contains("program") || message.Contains("app"))
            {
                return "💻 Software problems? I understand how frustrating that can be! Try:\n\n1️⃣ Closing and reopening the application\n2️⃣ Restarting your computer\n3️⃣ Checking for software updates\n\nWhat specific software are you having trouble with? I can search our knowledge base for more detailed solutions!";
            }

            // General conversation
            if (message.Contains("how are you") || message.Contains("how do you do"))
            {
                return "I'm doing fantastic! 🤖✨ I'm always energized and ready to help solve IT problems. How are you doing? Is there anything tech-related I can help you with today?";
            }

            // Search knowledge base for any other queries
            var generalResults = await SearchKnowledgeBase(userMessage);
            if (generalResults.Any())
            {
                var result = generalResults.First();
                return $"I found something that might help! 🔍\n\n**{result.Title}**\n{result.Summary}\n\nDoes this answer your question, or would you like me to search for something more specific?";
            }

            // Default response with knowledge base search offer
            return "I want to help you, but I need a bit more information! 🤔 Could you tell me more about:\n\n• What specific issue you're experiencing?\n• What type of technology or software is involved?\n• What you were trying to do when the problem occurred?\n\nOr I can search our knowledge base - just let me know what topic you'd like me to look up! 📚";
        }

        private async Task<List<KnowledgeBaseResult>> SearchKnowledgeBase(string query)
        {
            try
            {
                var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(term => term.Length > 2)
                    .Take(5);

                var results = new List<KnowledgeBaseResult>();

                var articles = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Where(kb => searchTerms.Any(term =>
                        kb.Title.ToLower().Contains(term) ||
                        kb.Content.ToLower().Contains(term)))
                    .Take(3)
                    .ToListAsync();

                foreach (var article in articles)
                {
                    results.Add(new KnowledgeBaseResult
                    {
                        ArticleId = article.ArticleID,
                        Title = article.Title,
                        Summary = article.Content.Length > 200
                            ? article.Content.Substring(0, 200) + "..."
                            : article.Content,
                        CategoryName = article.Category?.CategoryName ?? "General"
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching knowledge base");
                return new List<KnowledgeBaseResult>();
            }
        }

        private string GetPersonalizedWelcome(int? userID)
        {
            var welcomeMessages = new[]
            {
                "Hello! 👋 I'm your friendly IT Support Assistant! I'm here 24/7 to help you solve technology problems, answer questions, and make your digital life easier. What can I help you with today?",
                "Hi there! 🌟 Welcome to Omnitak IT Support! I'm your virtual assistant, ready to tackle any tech challenges you might have. From password resets to software troubleshooting, I've got you covered!",
                "Greetings! 🤖 I'm your personal IT helper, powered by advanced AI and filled with knowledge about all things tech. Whether you need quick fixes or detailed guidance, I'm here to assist!",
                "Hey! ✨ I'm your smart IT companion! I can help you solve problems, search our knowledge base, and even create support tickets when needed. What technology puzzle can we solve together today?"
            };

            var random = new Random();
            return welcomeMessages[random.Next(welcomeMessages.Length)];
        }

        private async Task<ChatbotConversation?> GetConversationAsync(string sessionID)
        {
            return await _context.ChatbotConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.SessionID == sessionID && c.Status == "Active");
        }

        private async Task SaveMessageAsync(int conversationID, string message, string messageType)
        {
            var chatMessage = new ChatbotMessage
            {
                ConversationID = conversationID,
                Message = message,
                MessageType = messageType,
                SentAt = DateTime.UtcNow
            };

            _context.ChatbotMessages.Add(chatMessage);
            await _context.SaveChangesAsync();
        }
    }


    public class ConversationContext
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime ConversationStartTime { get; set; }
        public int MessageCount { get; set; }
        public int GibberishCount { get; set; }
        public string LastUserMessage { get; set; } = string.Empty;
        public string LastBotResponse { get; set; } = string.Empty;
        public List<string> ConversationHistory { get; set; } = new();
    }

    public class KnowledgeBaseResult
    {
        public int ArticleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}