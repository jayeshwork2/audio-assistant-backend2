# Frontend Implementation Guide - Phase 1

This guide provides detailed specifications for implementing the frontend extension (audio-assistant-extension2) to integrate with the Phase 1 backend.

---

## Overview

The frontend is a Chrome/Browser extension built with React + TypeScript that provides a popup interface for:
- Recording audio
- Transcribing speech
- Generating AI responses
- Translating text
- Viewing meeting intelligence
- Exporting meetings

---

## Backend API Integration

### Base URL
```typescript
const API_BASE_URL = 'http://localhost:5000/api';
// or production URL
```

### Authentication
All API requests (except login/register) require JWT token:
```typescript
headers: {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
}
```

---

## Required Frontend Components

### 1. AIResponseDisplay.tsx
**Purpose:** Display AI-generated responses with typewriter animation

**Props:**
```typescript
interface AIResponseDisplayProps {
  response: string;
  provider: string;
  tokensUsed: number;
  timestamp: Date;
  isLoading: boolean;
  error?: string;
  onRetry?: () => void;
}
```

**Features:**
- Typewriter animation (character-by-character or word-by-word)
- Copy-to-clipboard button
- Show provider badge (Claude, GPT-4, Gemini)
- Display token count
- Loading spinner
- Error state with retry button
- Dark mode support

**API Integration:**
```typescript
const response = await fetch(`${API_BASE_URL}/response/generate`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    transcript: "User's transcribed text",
    conversationId: 123, // optional
    responseStyle: "formal", // optional
    aiProvider: "claude" // optional
  })
});
```

---

### 2. ResponseStyleSelector.tsx
**Purpose:** Allow users to select AI response style

**Props:**
```typescript
interface ResponseStyleSelectorProps {
  selectedStyle: string;
  onStyleChange: (style: string) => void;
  styles: ResponseStyle[];
}

interface ResponseStyle {
  name: string;
  displayName: string;
  description: string;
}
```

**Features:**
- Dropdown with 6 styles
- Quick-select buttons
- Style preview/sample
- Save as favorite
- Display style badge on responses

**API Integration:**
```typescript
// Get available styles
const styles = await fetch(`${API_BASE_URL}/response/styles`, {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

**Styles:**
1. Formal - "Professional, business-appropriate tone"
2. Casual - "Friendly, conversational tone"
3. Technical - "Detailed technical explanations"
4. ELI5 - "Simple explanations anyone can understand"
5. Funny - "Humorous, entertaining responses"
6. Bullet Points - "Concise, structured format"

---

### 3. TranslationPanel.tsx
**Purpose:** Translate transcripts and responses

**Props:**
```typescript
interface TranslationPanelProps {
  originalText: string;
  onTranslate: (translatedText: string) => void;
}
```

**Features:**
- Source language selector (with auto-detect)
- Target language selector (50+ languages)
- Swap languages button
- Side-by-side display
- Copy translated text button
- Translation history

**API Integration:**
```typescript
// Translate text
const result = await fetch(`${API_BASE_URL}/translation/translate`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    text: "Hello world",
    targetLanguage: "es",
    sourceLanguage: "en" // or "auto"
  })
});

// Get supported languages
const languages = await fetch(`${API_BASE_URL}/translation/languages`, {
  headers: { 'Authorization': `Bearer ${token}` }
});

// Detect language
const detection = await fetch(`${API_BASE_URL}/translation/detect`, {
  method: 'POST',
  body: JSON.stringify({ text: "Hola mundo" })
});
```

---

### 4. LanguagePairSelector.tsx
**Purpose:** Select source and target languages

**Props:**
```typescript
interface LanguagePairSelectorProps {
  sourceLanguage: string;
  targetLanguage: string;
  languages: Language[];
  onSourceChange: (lang: string) => void;
  onTargetChange: (lang: string) => void;
  onSwap: () => void;
}
```

**Features:**
- Dropdown for 50+ languages
- Auto-detect option for source
- Swap button (↔)
- Popular languages at top
- Search/filter languages

---

### 5. MeetingDetection.tsx
**Purpose:** Display detected meeting characteristics

**Props:**
```typescript
interface MeetingDetectionProps {
  transcript: string;
  onDetectionComplete: (detection: MeetingDetection) => void;
}

interface MeetingDetection {
  meetingType: string; // interview, sales, training, etc.
  domain: string; // technical, business, legal, etc.
  formality: string; // formal, informal, mixed
  urgency: string; // low, medium, high
}
```

**Features:**
- Badge for meeting type
- Badge for domain
- Formality indicator
- Urgency indicator (color-coded)
- Auto-detect on transcript complete

**API Integration:**
```typescript
// Detect meeting type
const typeResponse = await fetch(`${API_BASE_URL}/meeting/detect-type`, {
  method: 'POST',
  body: JSON.stringify({ transcript: "..." })
});

// Detect domain
const domainResponse = await fetch(`${API_BASE_URL}/meeting/detect-domain`, {
  method: 'POST',
  body: JSON.stringify({ transcript: "..." })
});

// Detect formality
const formalityResponse = await fetch(`${API_BASE_URL}/meeting/detect-formality`, {
  method: 'POST',
  body: JSON.stringify({ transcript: "..." })
});

// Detect urgency
const urgencyResponse = await fetch(`${API_BASE_URL}/meeting/detect-urgency`, {
  method: 'POST',
  body: JSON.stringify({ transcript: "..." })
});
```

---

### 6. ExportPanel.tsx
**Purpose:** Export meetings in multiple formats

**Props:**
```typescript
interface ExportPanelProps {
  meetingId: number;
  onExportComplete: (format: string) => void;
}
```

**Features:**
- Export as PDF button
- Export as Markdown button
- Export as Plain Text button
- Email export button (future)
- Loading spinner during export
- Success message with download link
- Export history

**API Integration:**
```typescript
// Export as PDF (downloads HTML file)
const pdfBlob = await fetch(`${API_BASE_URL}/export/pdf/${meetingId}`, {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` }
});

// Export as Markdown
const mdBlob = await fetch(`${API_BASE_URL}/export/markdown/${meetingId}`, {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` }
});

// Export as Plain Text
const txtBlob = await fetch(`${API_BASE_URL}/export/text/${meetingId}`, {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` }
});

// Get export history
const history = await fetch(`${API_BASE_URL}/export/history`, {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

**Download Handler:**
```typescript
const downloadFile = (blob: Blob, filename: string) => {
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  window.URL.revokeObjectURL(url);
};
```

---

## Custom Hooks

### 1. useAIResponse.ts
```typescript
interface UseAIResponseReturn {
  generateResponse: (transcript: string, options?: Options) => Promise<void>;
  response: ResponseResult | null;
  isGenerating: boolean;
  error: string | null;
  retry: () => void;
}

const useAIResponse = (conversationId?: number): UseAIResponseReturn => {
  // Implementation
};
```

### 2. useTranslation.ts
```typescript
interface UseTranslationReturn {
  translateText: (text: string, targetLang: string, sourceLang?: string) => Promise<void>;
  detectLanguage: (text: string) => Promise<string>;
  languages: Language[];
  translatedText: string | null;
  isTranslating: boolean;
  error: string | null;
}

const useTranslation = (): UseTranslationReturn => {
  // Implementation
};
```

### 3. useMeeting.ts
```typescript
interface UseMeetingReturn {
  createMeeting: (conversationId: number, title: string) => Promise<Meeting>;
  detectMeetingType: (transcript: string) => Promise<string>;
  detectDomain: (transcript: string) => Promise<string>;
  generateNotes: (meetingId: number) => Promise<MeetingNotes>;
  meeting: Meeting | null;
  isLoading: boolean;
}

const useMeeting = (): UseMeetingReturn => {
  // Implementation
};
```

### 4. useExport.ts
```typescript
interface UseExportReturn {
  exportAsPdf: (meetingId: number) => Promise<void>;
  exportAsMarkdown: (meetingId: number) => Promise<void>;
  exportAsText: (meetingId: number) => Promise<void>;
  isExporting: boolean;
  error: string | null;
}

const useExport = (): UseExportReturn => {
  // Implementation
};
```

### 5. useConversation.ts
```typescript
interface UseConversationReturn {
  createConversation: (meetingType?: string, domain?: string) => Promise<Conversation>;
  addExchange: (conversationId: number, input: string, response: string) => Promise<void>;
  getHistory: (conversationId: number) => Promise<Exchange[]>;
  endConversation: (conversationId: number) => Promise<void>;
  conversation: Conversation | null;
}

const useConversation = (): UseConversationReturn => {
  // Implementation
};
```

---

## Updated App.tsx Structure

```typescript
const App: React.FC = () => {
  const [isRecording, setIsRecording] = useState(false);
  const [transcript, setTranscript] = useState('');
  const [conversationId, setConversationId] = useState<number | null>(null);
  const [meetingId, setMeetingId] = useState<number | null>(null);
  const [selectedStyle, setSelectedStyle] = useState('formal');
  
  const { generateResponse, response, isGenerating } = useAIResponse(conversationId);
  const { translateText, translatedText } = useTranslation();
  const { detectMeetingType, generateNotes } = useMeeting();
  const { exportAsPdf, exportAsMarkdown } = useExport();

  // Workflow:
  // 1. User records audio
  // 2. Transcribe to text
  // 3. Detect meeting characteristics
  // 4. Generate AI response
  // 5. Optionally translate
  // 6. Generate notes
  // 7. Export

  return (
    <div className="app">
      <RecordingButton 
        isRecording={isRecording} 
        onStartRecording={handleStartRecording}
        onStopRecording={handleStopRecording}
      />
      
      <TranscriptDisplay transcript={transcript} />
      
      <MeetingDetection transcript={transcript} />
      
      <ResponseStyleSelector 
        selectedStyle={selectedStyle}
        onStyleChange={setSelectedStyle}
      />
      
      <AIResponseDisplay 
        response={response?.response}
        provider={response?.provider}
        isLoading={isGenerating}
      />
      
      <TranslationPanel originalText={response?.response || transcript} />
      
      {meetingId && (
        <ExportPanel 
          meetingId={meetingId}
          onExportComplete={handleExportComplete}
        />
      )}
    </div>
  );
};
```

---

## Styling Guide

### Color Scheme
```css
:root {
  --primary-color: #3498db;
  --success-color: #2ecc71;
  --warning-color: #f39c12;
  --danger-color: #e74c3c;
  --text-primary: #2c3e50;
  --text-secondary: #7f8c8d;
  --background: #ecf0f1;
  --card-background: #ffffff;
}

[data-theme="dark"] {
  --text-primary: #ecf0f1;
  --text-secondary: #bdc3c7;
  --background: #2c3e50;
  --card-background: #34495e;
}
```

### Typewriter Animation
```css
@keyframes typewriter {
  from { width: 0; }
  to { width: 100%; }
}

.typewriter-text {
  display: inline-block;
  overflow: hidden;
  white-space: nowrap;
  animation: typewriter 2s steps(40) forwards;
}
```

### Badge Styles
```css
.meeting-badge {
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.badge-interview { background: #3498db; color: white; }
.badge-sales { background: #2ecc71; color: white; }
.badge-technical { background: #9b59b6; color: white; }
.badge-urgent { background: #e74c3c; color: white; }
```

---

## Testing Requirements

### Component Tests
```typescript
// Example: AIResponseDisplay.test.tsx
describe('AIResponseDisplay', () => {
  it('should display response with typewriter effect', () => {});
  it('should show loading spinner when isLoading is true', () => {});
  it('should display error and retry button on error', () => {});
  it('should copy text to clipboard on button click', () => {});
  it('should display provider badge', () => {});
});
```

### Hook Tests
```typescript
// Example: useAIResponse.test.ts
describe('useAIResponse', () => {
  it('should generate response successfully', async () => {});
  it('should handle API errors gracefully', async () => {});
  it('should retry on failure', async () => {});
  it('should include conversation context', async () => {});
  it('should apply response style', async () => {});
});
```

---

## Development Workflow

1. **Setup:**
   ```bash
   npm install
   npm run dev
   ```

2. **Configure API URL:**
   Create `.env`:
   ```
   VITE_API_BASE_URL=http://localhost:5000/api
   ```

3. **Load Extension:**
   - Build: `npm run build`
   - Load `dist` folder in Chrome Extensions

4. **Test Workflow:**
   - Register/Login
   - Store API keys (Claude, OpenAI, Google)
   - Record audio → Transcribe
   - Generate AI response
   - Translate response
   - View meeting detection
   - Generate notes
   - Export as Markdown/PDF

---

## Package Dependencies

```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "typescript": "^5.0.0",
    "@types/react": "^18.2.0",
    "@types/react-dom": "^18.2.0"
  },
  "devDependencies": {
    "@vitejs/plugin-react": "^4.0.0",
    "vite": "^5.0.0",
    "vitest": "^1.0.0",
    "@testing-library/react": "^14.0.0",
    "@testing-library/jest-dom": "^6.0.0"
  }
}
```

---

## Error Handling

All API calls should handle:
- 401 Unauthorized → Redirect to login
- 400 Bad Request → Display error message
- 500 Server Error → Display "Service unavailable" with retry
- Network Error → Display "Connection lost" with retry

```typescript
const handleApiError = (error: any) => {
  if (error.status === 401) {
    // Clear token, redirect to login
    localStorage.removeItem('token');
    window.location.href = '/login';
  } else if (error.status === 400) {
    // Show validation error
    setError(error.message);
  } else {
    // Show generic error
    setError('An unexpected error occurred. Please try again.');
  }
};
```

---

## Next Steps

1. ✅ Backend complete (this repository)
2. ⏳ Frontend implementation (separate repository)
3. ⏳ Integration testing
4. ⏳ Chrome Web Store publication

---

## Support

For backend API questions, see:
- PHASE1_IMPLEMENTATION.md
- BACKEND_CHECKLIST.md
- Swagger UI: http://localhost:5000/swagger

---

**Frontend Status:** Ready for implementation
**Backend Status:** ✅ Complete and ready for integration
