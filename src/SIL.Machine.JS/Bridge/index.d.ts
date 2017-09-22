export interface SmtTrainProgress {
    readonly currentStep: number;
    readonly currentStepMessage: string;
    readonly stepCount: number;
    readonly percentCompleted: number;
}

export interface InteractiveTranslationSession {
    readonly sourceSegment: string[];
    confidenceThreshold: number;
    readonly prefix: string[];
    readonly isLastWordComplete: boolean;
    readonly currentSuggestion: string[];
    updatePrefix(prefix: string): string[];
    getSuggestionTextInsertion(suggestionIndex?: number): { deleteLength: number, insertText: string };
    updateSuggestion(): void;
    approve(onFinished: { (arg: boolean): void }): void;
}

export class TranslationEngine {
    constructor(baseUrl: string, projectId: string);
    translateInteractively(sourceSegment: string, confidenceThreshold: number,
        onFinished: { (arg: InteractiveTranslationSession): void }): void;
    train(onStatusUpdate: { (arg: SmtTrainProgress): void },
        onFinished: { (arg: boolean): void }): void;
    startTraining(onFinished: { (arg: boolean): void }): void;
    listenForTrainingStatus(onStatusUpdate: { (arg: SmtTrainProgress): void },
        onFinished: { (arg: boolean): void }): void;
}

export interface Range {
    start: number;
    end: number;
    length: number;
}

export class SegmentTokenizer {
    constructor(segmentType: string);
    tokenize(text: string): Range[];
    tokenizeToStrings(text: string): string[];
}
