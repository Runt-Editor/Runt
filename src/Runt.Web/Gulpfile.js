var gulp = require('gulp'),
    watch = require('gulp-watch'),
    rjs = require('gulp-requirejs'),
    plumber = require('gulp-plumber'),
    tcs = require('gulp-typescript-compiler'),
    clean = require('gulp-clean');

gulp.task('build-ts', function() {
  return gulp.src('ts/**/*.js')
  .pipe(tcs({
    sourcemaps: true,
    module: 'amd'
  }))
  .pipe(gulp.dest('js'));
});

gulp.task('_rjs', ['build-ts'], function() {
  rjs({
    baseUrl: './js/',
    name: 'app',
    out: 'app.js'
  }).pipe(gulp.dest('tmp'));
});

gulp.task('_clean-js', ['_rjs'], function() {
  return gulp.src('js', {read: false})
    .pipe(clean({force: true}));
});

gulp.task('_moveBack', ['_clean-js'], function() {
  return gulp.src('tmp/app.js', {base: './tmp'})
    .pipe(gulp.dest('js'));
});

gulp.task('_cleanTmp', ['_moveBack'], function() {
  return gulp.src('tmp', {read: false})
    .pipe(clean({force: true}));
});

gulp.task('build', ['_moveBack', '_cleanTmp']);

gulp.task('watch', function() {
  return watch({glob: 'ts/**/*.ts'})
    .pipe(plumber())
    .pipe(tcs({
      sourcemaps: true,
      module: 'amd'
    }))
    .pipe(gulp.dest('js'));
});

gulp.task('default', ['build']);