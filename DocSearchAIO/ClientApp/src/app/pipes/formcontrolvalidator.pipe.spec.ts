import {FormControlValidatorPipe} from "./fomcontrolvalidator.pipe";
import {AbstractControl, FormControl, UntypedFormControl, Validators} from "@angular/forms";

describe('FormcontrolValidator', () => {
  const pipe = new FormControlValidatorPipe();

  it('generates', () => {
    expect(pipe).toBeTruthy();
  });

  it('transforms null to be true', () => {
    expect(pipe.transform(null)).toBeTrue();
  });

  it('transforms false if control is invalid', () => {
    const testee = new FormControl();
    testee.addValidators([Validators.maxLength(1)]);
    testee.setValue("12345");
    expect(testee.valid).toBeFalse();
    expect(pipe.transform(testee)).toBeFalse();
  })

})
