import {FormControlConverterPipe} from "./formcontrolconverter.pipe";
import {AbstractControl, UntypedFormControl} from "@angular/forms";

describe('FormControlConverterPipe', () => {
  const pipe = new FormControlConverterPipe();

  const testee: AbstractControl = new UntypedFormControl();

  it('generates', () => {
    expect(pipe).toBeTruthy();
  })

  it('transforms', () => {
    expect(pipe.transform(testee)).toBeInstanceOf(UntypedFormControl);
  });

  it('transforms nothing if null', () => {
    expect(pipe.transform(null)).toBeNull();
  });
})
