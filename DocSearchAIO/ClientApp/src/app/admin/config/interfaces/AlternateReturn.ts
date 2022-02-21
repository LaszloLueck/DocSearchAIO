export class AlternateReturn{
  hasError: boolean;
  errorMessage: string;

  constructor(hasError: boolean, errorMessage: string) {
    this.hasError = hasError;
    this.errorMessage = errorMessage;
  }

}
