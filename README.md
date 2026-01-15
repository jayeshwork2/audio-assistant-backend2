# Audio Assistant API - Phase 1 Complete

A comprehensive .NET 8 Web API for the Audio Assistant application, providing authentication, API key management, AI-powered speech-to-text transcription, AI response generation, translation, meeting intelligence, and export capabilities.

## 🎉 Phase 1 Features - COMPLETE

### Core Features

- **User Authentication**: Secure registration and login with JWT tokens
- **Password Security**: BCrypt-based password hashing with salt
- **API Key Management**: Encrypted storage of external provider API keys (AES-256)
- **STT Service Abstraction**: Multi-provider transcription with automatic fallback
  - Groq Whisper (primary, server-configured)
  - Whisper.cpp (local fallback)
  - OpenAI Whisper (user-provided API key)
  - Claude Haiku STT (user-provided API key)
- **Database**: SQLite with Entity Framework Core
- **Middleware**: Error handling, rate limiting, and JWT authentication
- **Logging**: Structured logging with Serilog
- **API Documentation**: Interactive Swagger/OpenAPI documentation
- **CORS Support**: Configurable cross-origin resource sharing

### 🆕 AI Response Generation

- **Multi-Provider AI**: Claude Haiku, GPT-4, Gemini with automatic fallback
- **6 Response Styles**: Formal, Casual, Technical, ELI5, Funny, Bullet Points
- **Conversation Context**: Maintains last 10 exchanges for context-aware responses
- **Token Tracking**: Monitors token usage and API costs
- **Dynamic Prompts**: Style-based system prompt modification

### 🆕 Translation (50+ Languages)

- **Language Detection**: Automatic detection using heuristics + Google Translate API
- **50+ Languages**: English, Spanish, French, German, Chinese, Arabic, Japanese, and many more
- **Translation Caching**: Reduces API calls by caching translations
- **Translation History**: Track all translations per transcript

### 🆕 Conversation Management

- **Session Tracking**: Multi-turn conversation sessions
- **Exchange History**: Stores user input and AI responses with timestamps
- **Context Aggregation**: Builds context from recent exchanges for better AI responses
- **Meeting Association**: Links conversations to meeting metadata

### 🆕 Meeting Intelligence

- **Type Detection**: Interview, Sales, Training, Standup, General Meeting
- **Domain Detection**: Technical, Business, Legal, Medical, Finance, Marketing, HR
- **Formality Analysis**: Formal, Informal, Mixed
- **Urgency Detection**: Low, Medium, High
- **Smart Notes**: Automatic summary, key points, action items, decisions
- **Action Item Extraction**: Automatically identifies tasks and TODOs
- **Key Point Identification**: Extracts important discussion points

### 🆕 Export Capabilities

- **PDF Export**: Beautiful HTML format optimized for PDF printing
- **Markdown Export**: Well-structured markdown with headers and lists
- **Plain Text Export**: Clean formatted text
- **Export History**: Tracks all exports per user
- **Includes**: Meeting metadata, summary, key points, action items, full transcript

### Transcription Features

- **Multi-Provider Support**: Groq, Whisper.cpp, OpenAI, Claude
- **Automatic Fallback**: Seamless provider switching on failure
- **User Preferences**: Configurable preferred provider
- **Language Support**: 24+ languages supported
- **Error Handling**: Graceful degradation and retry logic
- **Cost Tracking**: Logging of provider usage and costs
- **Provider API**: Get available providers and set preferences

## Tech Stack

- **.NET 8.0**: Latest LTS version of .NET
- **Entity Framework Core 8.0**: ORM for database operations
- **SQLite**: Lightweight database for development and production
- **BCrypt.Net-Next**: Password hashing library
- **JWT Bearer Authentication**: Secure token-based authentication
- **Serilog**: Structured logging framework
- **Swagger/OpenAPI**: API documentation and testing

## Project Structure

```
AudioAssistant.Api/
├── Controllers/          # API endpoints (8 controllers)
│   ├── AuthController.cs
│   ├── ApiKeyController.cs
│   ├── TranscriptionController.cs
│   ├── ResponseController.cs       # 🆕 AI response generation
│   ├── TranslationController.cs    # 🆕 Translation
│   ├── ConversationController.cs   # 🆕 Conversation management
│   ├── MeetingController.cs        # 🆕 Meeting intelligence
│   └── ExportController.cs         # 🆕 Export functionality
├── Services/            # Business logic (10 services)
│   ├── Abstractions/
│   │   └── ITranscriptionProvider.cs
│   ├── Providers/
│   │   ├── GroqWhisperProvider.cs
│   │   ├── WhisperCppProvider.cs
│   │   ├── OpenAIWhisperProvider.cs
│   │   └── ClaudeHaikuSTTProvider.cs
│   ├── AuthService.cs
│   ├── ApiKeyService.cs
│   ├── TranscriptionService.cs
│   ├── ResponseService.cs          # 🆕 AI responses
│   ├── TranslationService.cs       # 🆕 Translation
│   ├── ConversationService.cs      # 🆕 Conversations
│   ├── MeetingService.cs           # 🆕 Meeting intelligence
│   └── ExportService.cs            # 🆕 Export
├── Models/              # Data models and DTOs
│   ├── User.cs
│   ├── UserPreferences.cs          # 🆕 Updated with export format
│   ├── ApiKey.cs
│   ├── Transcript.cs
│   ├── Translation.cs              # 🆕 Restructured
│   ├── Conversation.cs
│   ├── ConversationExchange.cs
│   ├── Meeting.cs
│   ├── MeetingNotes.cs             # 🆕 Updated with decisions
│   ├── Export.cs
│   ├── TransactionLog.cs
│   └── DTOs/                       # 11+ DTOs
├── Data/                # Database context
│   └── AudioAssistantDbContext.cs
├── Middleware/          # Custom middleware
│   ├── ErrorHandlingMiddleware.cs
│   └── RateLimitingMiddleware.cs
├── Utilities/           # Helper classes
│   ├── PasswordHasher.cs
│   ├── JwtTokenGenerator.cs
│   ├── EncryptionService.cs
│   ├── PdfGenerator.cs             # 🆕 PDF/HTML generation
│   └── MarkdownFormatter.cs        # 🆕 Markdown & text formatting
├── Migrations/          # EF Core migrations
│   └── 20260115000000_AddPhase1Features.cs  # 🆕 Phase 1 schema
└── Tests/               # 49 comprehensive tests
    ├── ResponseServiceTests.cs      # 🆕 10 tests
    ├── TranslationServiceTests.cs   # 🆕 8 tests
    ├── ConversationServiceTests.cs  # 🆕 10 tests
    ├── MeetingServiceTests.cs       # 🆕 13 tests
    ├── ExportServiceTests.cs        # 🆕 8 tests
    └── IntegrationTests.cs          # 🆕 3 integration tests
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- A code editor (Visual Studio, VS Code, or JetBrains Rider)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd AudioAssistant.Api
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Update configuration**
   
   Edit `appsettings.Development.json` to customize settings (optional):
   ```json
   {
     "JwtSettings": {
       "SecretKey": "your-secret-key-here-at-least-32-characters",
       "ExpirationMinutes": 1440
     },
     "EncryptionSettings": {
       "EncryptionKey": "your-encryption-key-here"
     }
   }
   ```

4. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```
   
   Or simply run the application - migrations will be applied automatically on startup.

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the API**
   - API: `https://localhost:5001` or `http://localhost:5000`
   - Swagger UI: `https://localhost:5001/swagger`
   - Health Check: `https://localhost:5001/health`

## API Endpoints

### Authentication

#### Register a new user
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@example.com"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@example.com"
}
```

#### Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### API Key Management

All API key endpoints require authentication. Include the JWT token in the Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Store API Key
```http
POST /api/apikey/store
Authorization: Bearer {token}
Content-Type: application/json

{
  "provider": "OpenAIWhisper",
  "apiKey": "sk-xxxxxxxxxxxx"
}
```

#### Get Configured Providers
```http
GET /api/apikey/providers
Authorization: Bearer {token}
```

**Response:**
```json
{
  "providers": ["OpenAIWhisper"]
}
```

#### Delete API Key
```http
DELETE /api/apikey/{provider}
Authorization: Bearer {token}
```

### Transcription

All transcription endpoints require authentication.

#### Transcribe Audio
```http
POST /api/transcribe
Authorization: Bearer {token}
Content-Type: application/json

{
  "audioData": [base64 encoded byte array],
  "language": "en",
  "provider": "GroqWhisper",
  "streaming": false
}
```

**Response:**
```json
{
  "id": "guid",
  "text": "Transcribed text",
  "language": "en",
  "confidence": 0.95,
  "duration": "00:00:01.234",
  "provider": "GroqWhisper",
  "tokens": 100,
  "timestamp": "2024-01-15T10:30:00Z",
  "usedFallback": false
}
```

#### Get Available Providers
```http
GET /api/transcribe/providers
Authorization: Bearer {token}
```

**Response:**
```json
["GroqWhisper", "WhisperCpp", "OpenAIWhisper"]
```

#### Set Preferred Provider
```http
POST /api/transcribe/preferences/provider
Authorization: Bearer {token}
Content-Type: application/json

{
  "provider": "GroqWhisper"
}
```

## Database Schema

The application uses SQLite with the following main entities:

- **User**: User accounts with email and password
- **UserPreferences**: User-specific settings and preferences
- **ApiKey**: Encrypted storage for external API keys
- **Conversation**: Conversation sessions
- **ConversationExchange**: Individual exchanges within conversations
- **Transcript**: Audio transcripts
- **Translation**: Translated transcripts
- **Meeting**: Meeting metadata
- **MeetingNotes**: Meeting notes and summaries
- **Export**: Exported meeting notes
- **TransactionLog**: API usage tracking

## Configuration

### JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-minimum-32-characters",
    "Issuer": "AudioAssistant.Api",
    "Audience": "AudioAssistant.Client",
    "ExpirationMinutes": 1440
  }
}
```

### Rate Limiting
```json
{
  "RateLimiting": {
    "RequestsPerMinute": 100
  }
}
```

### CORS
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "chrome-extension://*",
      "http://localhost:3000"
    ]
  }
}
```

### Transcription Providers
```json
{
  "GroqSettings": {
    "ApiKey": "your-groq-api-key",
    "Endpoint": "https://api.groq.com/openai/v1"
  },
  "WhisperCppSettings": {
    "Endpoint": "http://localhost:8080",
    "Model": "base"
  },
  "OpenAISettings": {
    "Endpoint": "https://api.openai.com/v1"
  },
  "TranscriptionSettings": {
    "DefaultProvider": "GroqWhisper",
    "FallbackChain": ["GroqWhisper", "WhisperCpp", "OpenAIWhisper"],
    "MaxRetries": 2,
    "RetryDelaySeconds": 1
  }
}
```

**Provider Setup:**

1. **Groq Whisper** (Primary):
   - Sign up at https://console.groq.com/
   - Get API key from settings
   - Configure in appsettings.json

2. **Whisper.cpp** (Local Fallback):
   - Install whisper.cpp: https://github.com/ggerganov/whisper.cpp
   - Run server: `./server -m base.bin -p 8080`
   - Configure endpoint in appsettings.json

3. **OpenAI Whisper** (User-provided):
   - Users store their own API keys via `/api/apikey/store`
   - Provider name: `OpenAIWhisper`

## Security Best Practices

1. **Never commit secrets**: Use environment variables for sensitive data in production
2. **Change default keys**: Update JWT and encryption keys in production
3. **Use HTTPS**: Always use HTTPS in production
4. **Rate limiting**: Adjust rate limits based on your needs
5. **Password requirements**: Minimum 8 characters enforced

## Development

### Running Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

Remove last migration:
```bash
dotnet ef migrations remove
```

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Troubleshooting

### Database Issues

If you encounter database errors, try:
1. Delete the database file (`audioassistant.dev.db`)
2. Delete the `Migrations` folder
3. Run `dotnet ef migrations add InitialCreate`
4. Run the application

### JWT Token Issues

- Ensure the JWT secret key is at least 32 characters
- Check token expiration settings
- Verify the token is included in the Authorization header as `Bearer {token}`

### CORS Issues

Update the `CorsSettings:AllowedOrigins` in `appsettings.json` to include your client application's origin.

## License

[Your License Here]

## Support

For issues and questions, please create an issue in the repository.
