

export interface SearchResponse {
  searchResults: SearchResult[],
  searchResult: NavigationResult,
  statistics: SearchStatistic
}

export interface SearchResult {
  relativeUrl: string,
  id: string,
  absoluteUrl: string,
  documentType: string,
  contents: Content[],
  comments: Comment[],
  relevance: number,
  programIcon: string,
  processTime: Date
}

export interface Content {
  contentText: string
}

export interface Comment {
  commentText: string,
  author: string,
  date: Date;
  id: string,
  initials: string
}

export interface NavigationResult {
  currentPage: number,
  currentPageSize: number,
  docCount: number,
  searchPhrase: string
}

export interface SearchStatistic {
  searchTime: number,
  docCount: number
}
