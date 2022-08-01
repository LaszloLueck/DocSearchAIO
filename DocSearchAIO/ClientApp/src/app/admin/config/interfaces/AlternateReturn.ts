export class AlternateReturn{
  errorMessage: string;
  operation: string;
  errorCode: number;

  constructor(errorMessage: string, operation: string, errorCode: number) {
    this.errorMessage = errorMessage;
    this.operation = operation;
    this.errorCode = errorCode;
  }

}
