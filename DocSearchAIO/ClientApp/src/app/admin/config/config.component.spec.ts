import {ComponentFixture, TestBed} from "@angular/core/testing";
import {ConfigComponent} from "./config.component";
import {ReactiveFormsModule} from "@angular/forms";
import {CommonDataService} from "../../services/CommonDataService";
import {ConfigApiService} from "./config-api.service";
import {RouterTestingModule} from "@angular/router/testing";
import {of} from "rxjs";
import {BaseError, DocSearchConfiguration} from "./interfaces/DocSearchConfiguration";
import {Either, makeLeft} from "./Either";

describe('ConfigComponent', () => {
  let component: ConfigComponent;
  let fixture: ComponentFixture<ConfigComponent>;
  let fakeConfigApiService: ConfigApiService;

  beforeEach(async() => {
    const err: BaseError = {errorMessage: 'errorOperationMessage', errorCode: 999, operation: "errorOperation"};
    const bla: Either<BaseError, DocSearchConfiguration> = makeLeft(err);


    fakeConfigApiService = jasmine.createSpyObj<ConfigApiService>('ConfigApiService', {
      getConfiguration: of(bla),
      setConfiguration: undefined,
      }
    )

    await TestBed.configureTestingModule(
      {
        declarations: [ConfigComponent],
        imports: [ReactiveFormsModule, RouterTestingModule],
        providers: [
          CommonDataService,
          {provide: ConfigApiService, useValue: fakeConfigApiService}
        ]
      }
    ).compileComponents()

    fixture = TestBed.createComponent(ConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should called getConfiguration', () => {
    expect(fakeConfigApiService.getConfiguration).toHaveBeenCalled();
  });

  it('should filled up the alternate object (in case of error)', () => {
    expect(component.alternateReturn).toBeTruthy();
  });

  it('should not filled the specific return object in case of an error', () => {
    expect(component.configuration).not.toBeTruthy();
  });

  it('should return an error object with a specific value to component, when the service returned an error', () => {
    expect(component.alternateReturn.errorCode).toBe(999);
    expect(component.alternateReturn.operation).toBe('errorOperation');
    expect(component.alternateReturn.errorMessage).toBe('errorOperationMessage');
  });

});
