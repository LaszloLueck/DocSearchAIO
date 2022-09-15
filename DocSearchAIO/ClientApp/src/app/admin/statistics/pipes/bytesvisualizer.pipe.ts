import { Pipe, PipeTransform } from '@angular/core';

export type ByteUnit = 'B' | 'kB' | 'KB' | 'MB' | 'GB' | 'TB';

@Pipe({
  name: 'bytesvisualizer'
})
export class BytesvisualizerPipe implements PipeTransform {

  static formats: { [key: string]: { max: number; prev?: ByteUnit } } = {
    B: { max: 1024 },
    kB: { max: Math.pow(1024, 2), prev: 'B' },
    KB: { max: Math.pow(1024, 2), prev: 'B' }, // Backward compatible
    MB: { max: Math.pow(1024, 3), prev: 'kB' },
    GB: { max: Math.pow(1024, 4), prev: 'MB' },
    TB: { max: Number.MAX_SAFE_INTEGER, prev: 'GB' },
  };

  transform(input: any, decimal: number = 0, from: ByteUnit = 'B', to?: ByteUnit): any {
    if (!(BytesvisualizerPipe.isNumberFinite(input) && BytesvisualizerPipe.isNumberFinite(decimal) && BytesvisualizerPipe.isInteger(decimal) && BytesvisualizerPipe.isPositive(decimal))) {
      return input;
    }

    let bytes = input;
    let unit = from;
    while (unit !== 'B') {
      bytes *= 1024;
      unit = BytesvisualizerPipe.formats[unit].prev!;
    }

    if (to) {
      const format = BytesvisualizerPipe.formats[to];

      const result = BytesvisualizerPipe.toDecimal(BytesvisualizerPipe.calculateResult(format, bytes), decimal);

      return BytesvisualizerPipe.formatResult(result, to);
    }

    for (const key in BytesvisualizerPipe.formats) {
      if (BytesvisualizerPipe.formats.hasOwnProperty(key)) {
        const format = BytesvisualizerPipe.formats[key];
        if (bytes < format.max) {
          const result = BytesvisualizerPipe.toDecimal(BytesvisualizerPipe.calculateResult(format, bytes), decimal);

          return BytesvisualizerPipe.formatResult(result, key);
        }
      }
    }
  }

  static toDecimal(value: number, decimal: number): number {
    return Math.round(value * Math.pow(10, decimal)) / Math.pow(10, decimal);
  }

  static isPositive(value: number): boolean {
    return value >= 0;
  }

  static isInteger(value: number): boolean {
    // No rest, is an integer
    return value % 1 === 0;
  }

   static isNumber(value: any): value is number {
    return typeof value === 'number';
  }

  static isNumberFinite(value: any): value is number {
    return BytesvisualizerPipe.isNumber(value) && isFinite(value);
  }

  static formatResult(result: number, unit: string): string {
    return `${result} ${unit}`;
  }

  static calculateResult(format: { max: number; prev?: ByteUnit }, bytes: number) {
    const prev = format.prev ? BytesvisualizerPipe.formats[format.prev] : undefined;
    return prev ? bytes / prev.max : bytes;
  }

}
