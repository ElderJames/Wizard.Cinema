import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { _HttpClient } from '@delon/theme';

const provinces = [{
  value: 'zhejiang',
  label: 'Zhejiang'
}, {
  value: 'jiangsu',
  label: 'Jiangsu'
}];

const cities = {
  zhejiang: [{
    value: 'hangzhou',
    label: 'Hangzhou'
  }, {
    value: 'ningbo',
    label: 'Ningbo',
    isLeaf: true
  }],
  jiangsu: [{
    value: 'nanjing',
    label: 'Nanjing'
  }]
};

const scenicspots = {
  hangzhou: [{
    value: 'xihu',
    label: 'West Lake',
    isLeaf: true
  }],
  nanjing: [{
    value: 'zhonghuamen',
    label: 'Zhong Hua Men',
    isLeaf: true
  }]
};

@Component({
  selector: 'session-edit',
  templateUrl: './session-edit.component.html',
})
export class SessionEditComponent implements OnInit {
  form: FormGroup;
  submitting = false;
  divisions: any[];
  selectCityId = 0;
  modeldata = {
    sessionId: null,
    divisionId: 0,
    cinemaId: 0,
    hallId: 0,
    seats: []
  };

  constructor(
    private fb: FormBuilder,
    private msg: NzMessageService,
    private route: ActivatedRoute,
    private router: Router,
    private http: _HttpClient,
  ) { }

  ngOnInit(): void {
    this.form = this.fb.group({
      divisionId: [null, [Validators.required]],
      cinemaId: [null, [Validators.required]],
      hall: [null, [Validators.required]],
      seats: [null, [Validators.required]],
    });

    this.getDivisions();

    this.route.params.subscribe((params: Params) => {
      const sessionId = params['id'];
      console.log(sessionId);
      if (!sessionId)
        return;
      this.getActivity(sessionId);
      this.modeldata.sessionId = sessionId;
    });
  }

  getActivity(id: number) {
    this.http.get('api/session/' + id).subscribe((res: any) => {
      this.modeldata = res;
      console.log(res);
      for (const key in res) {
        const val = res[key];
        if (this.form.controls[key]) this.form.controls[key].setValue(val);
      }

      var hallarr = [];
      hallarr.push(this.modeldata.cinemaId);
      hallarr.push(this.modeldata.hallId);
      this.form.controls['hall'].setValue(hallarr);
    });
  }

  getDivisions() {
    this.http.get('api/division', { PageSize: 1000 })
      .subscribe((res: any) => {
        this.divisions = res.records;
      })
  }

  onDivisionChanges(value: any) {

    if (!this.divisions)
      return;

    let selected = this.divisions.find(x => x.divisionId == value);
    if (!selected)
      return;

    this.selectCityId = selected.cityId;
    console.log(this.selectCityId);
  }

  submit() {
    for (const i in this.form.controls) {
      this.form.controls[i].markAsDirty();
      this.form.controls[i].updateValueAndValidity();
      this.modeldata[i] = this.form.controls[i].value;
    }

    this.modeldata.cinemaId = this.form.controls['hall'][0];
    this.modeldata.hallId = this.form.controls['hall'][1];

    if (this.form.invalid) return;
    console.log(this.modeldata);
    // this.submitting = true;

    // this.submitting = this.http.loading;
    // this.http.post('api/activity', this.modeldata).subscribe((res: any) => {
    //   this.submitting = false;
    //   this.msg.success(`提交成功`);
    //   this.router.navigate(['activity']);
    // });
  }

  /** ngModel value */
  public values: any[] = null;

  public onChanges(values: any): void {
    console.log(values);
  }

  /** load data async execute by `nzLoadData` method */
  public loadData(node: any, index: number): PromiseLike<any> {

   // if (index < 0) { // if index less than 0 it is root node
      return this.http.get('api/city/' + this.selectCityId + '/cinemas', { size: 300 }).toPromise()
        .then((res: any) => {
          console.log(res);
          node.children = res.result.map(x => {
            return {
              value: x.cinemaId,
              label: x.name,
              isLeaf: true
            };
          });
        });
      // setTimeout(() => {
      //   if (index < 0) {
      //     node.children = provinces;
      //   } else if (index === 0) {
      //     node.children = cities[node.value];
      //   } else {
      //     node.children = scenicspots[node.value];
      //   }
      //   resolve();
      // }, 1000);
 //  }
  }
}
