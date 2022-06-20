import {SuggestResponseDetail} from "./SuggestResponseDetail";

export interface SuggestResponse {
  searchPhrase: string,
  suggests: SuggestResponseDetail[]
}
